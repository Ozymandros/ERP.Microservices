using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Infrastructure.Memory;
using MyApp.Agentic.Infrastructure.Secrets;
using MyApp.Agentic.Infrastructure.State;
using MyApp.Agentic.Domain.Skills;

namespace MyApp.Agentic.Application.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSessionRepository _sessionRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly ISecretStore _secretStore;
    private readonly ISessionStateStore _sessionStateStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly IAgentExecutionService _agentExecutionService;
    private readonly IMapper _mapper;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IAgentRepository agentRepository,
        IAgentSessionRepository sessionRepository,
        IMemoryRepository memoryRepository,
        ISecretStore secretStore,
        ISessionStateStore sessionStateStore,
        IEmbeddingService embeddingService,
        IAgentExecutionService agentExecutionService,
        IMapper mapper,
        ILogger<AgentService> logger)
    {
        _agentRepository = agentRepository;
        _sessionRepository = sessionRepository;
        _memoryRepository = memoryRepository;
        _secretStore = secretStore;
        _sessionStateStore = sessionStateStore;
        _embeddingService = embeddingService;
        _agentExecutionService = agentExecutionService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AgentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        return agent == null ? null : MapToDto(agent);
    }

    public async Task<IEnumerable<AgentListDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _agentRepository.GetAllAsync();
        return agents.Select(MapToListDto);
    }

    public async Task<IEnumerable<AgentListDto>> ListByTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var agents = await _agentRepository.GetAllAsync();
        var filtered = tenantId.HasValue
            ? agents.Where(a => a.TenantId == tenantId || a.TenantId == null)
            : agents;
        return filtered.Select(MapToListDto);
    }

    public async Task<AgentDto> CreateAsync(CreateAgentDto dto, CancellationToken cancellationToken = default)
    {
        var agent = new Agent(
            id: Guid.NewGuid(),
            name: dto.Name,
            description: dto.Description,
            modelId: dto.ModelId,
            temperature: dto.Temperature,
            systemInstructions: dto.SystemPrompt,
            tenantId: dto.TenantId,
            botType: dto.BotType,
            topK: dto.TopK,
            maxTokens: dto.MaxTokens,
            embeddingDimensions: dto.EmbeddingDimensions,
            enableMemory: dto.EnableMemory,
            enableRAG: dto.EnableRAG,
            embeddingModelName: dto.EmbeddingModelName);

        await _agentRepository.AddAsync(agent);
        return MapToDto(agent);
    }

    public async Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null) throw new InvalidOperationException($"Agent with ID {id} not found.");

        agent.Update(
            dto.Name,
            dto.Description,
            dto.ModelId,
            dto.Temperature,
            dto.SystemPrompt,
            dto.TopK,
            dto.MaxTokens,
            dto.EmbeddingDimensions,
            dto.EnableMemory,
            dto.EnableRAG,
            dto.EmbeddingModelName,
            dto.BotType);

        await _agentRepository.UpdateAsync(agent);
        return MapToDto(agent);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null) return;
        await _agentRepository.DeleteAsync(agent);
    }

    public async Task<ProcessAgentMessageResponse> ProcessMessageAsync(
        ProcessAgentMessageRequest request,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing message for Agent {AgentId}, User {UserId}", request.AgentId, authenticatedUserId);

        var agent = await _agentRepository.GetByIdWithDetailsAsync(request.AgentId, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with ID {request.AgentId} not found.");

        if (!agent.IsActive)
            throw new InvalidOperationException($"Agent {request.AgentId} is not active.");

        if (agent.TenantId.HasValue && agent.TenantId != tenantId)
            throw new UnauthorizedAccessException("User does not have access to this agent.");

        var provider = agent.Model?.Provider
            ?? throw new InvalidOperationException($"Agent {request.AgentId} has no model configured.");

        var apiKey = await _secretStore.GetSecretAsync(provider.SecretKeyName, cancellationToken)
            ?? throw new InvalidOperationException($"API key not found for provider {provider.Name}.");

        var sessionState = await _sessionStateStore.GetSessionAsync(request.AgentId, authenticatedUserId, cancellationToken);

        var effectiveTopK = request.Options?.TopK ?? agent.TopK;
        var effectiveTemperature = request.Options?.Temperature ?? agent.Temperature;
        var enableMemory = request.Options?.EnableMemory ?? agent.EnableMemory;
        var enableRAG = request.Options?.EnableRAG ?? agent.EnableRAG;

        var contextMemories = new List<string>();
        if (enableRAG && sessionState?.Messages.Any() == true)
        {
            var userEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message, cancellationToken);
            var similarMemories = await _memoryRepository.SearchSimilarAsync(
                sessionState.SessionId,
                userEmbedding,
                topK: effectiveTopK,
                cancellationToken);

            contextMemories = similarMemories.Select(m => m.Content).ToList();
        }

        var conversationHistory = sessionState?.Messages.Select(m => $"{m.Role}: {m.Content}").ToList() ?? new List<string>();

        var context = new AgentExecutionContext
        {
            Agent = agent,
            ApiKey = apiKey,
            BaseUrl = provider.BaseUrl,
            SystemPrompt = agent.SystemInstructions,
            ConversationHistory = conversationHistory,
            ContextMemories = contextMemories,
            Temperature = effectiveTemperature,
            MaxTokens = request.Options?.MaxTokens ?? agent.MaxTokens
        };

        var aiResponse = await _agentExecutionService.ExecuteAsync(context, request.Message, cancellationToken);

        var currentSessionId = sessionState?.SessionId ?? Guid.NewGuid();

        var userMessage = new ConversationMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };

        var assistantMessage = new ConversationMessage
        {
            Role = "assistant",
            Content = aiResponse,
            Timestamp = DateTime.UtcNow
        };

        await _sessionStateStore.AppendMessageAsync(request.AgentId, authenticatedUserId, userMessage, cancellationToken);
        await _sessionStateStore.AppendMessageAsync(request.AgentId, authenticatedUserId, assistantMessage, cancellationToken);

        if (enableMemory)
        {
            var userMessageEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message, cancellationToken);
            var responseEmbedding = await _embeddingService.GenerateEmbeddingAsync(aiResponse, cancellationToken);

            await _memoryRepository.AddMemoryAsync(
                new AgentMemory(Guid.NewGuid(), currentSessionId, MemoryRole.User, request.Message, userMessageEmbedding),
                cancellationToken);

            await _memoryRepository.AddMemoryAsync(
                new AgentMemory(Guid.NewGuid(), currentSessionId, MemoryRole.Assistant, aiResponse, responseEmbedding),
                cancellationToken);
        }

        return new ProcessAgentMessageResponse(currentSessionId, authenticatedUserId, request.Message, aiResponse, DateTime.UtcNow);
    }

    public async Task<StartSessionResponse> StartSessionAsync(
        StartSessionRequest request,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdWithDetailsAsync(request.AgentId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with ID {request.AgentId} not found.");

        if (!agent.IsActive)
            throw new InvalidOperationException($"Agent {request.AgentId} is not active.");

        if (agent.TenantId.HasValue && agent.TenantId != tenantId)
            throw new UnauthorizedAccessException("User does not have access to this agent.");

        var session = new AgentSession(
            id: Guid.NewGuid(),
            agentId: agent.Id,
            userId: authenticatedUserId,
            title: request.Title);

        await _sessionRepository.AddAsync(session);

        return new StartSessionResponse(
            session.Id,
            agent.Id,
            agent.Name,
            agent.BotType,
            authenticatedUserId,
            session.Title,
            session.StartedAt,
            session.Status);
    }

    public async Task<SendMessageResponse> SendMessageAsync(
        Guid sessionId,
        SendMessageRequest request,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithAgentAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session with ID {sessionId} not found.");

        if (session.UserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not own this session.");

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException($"Session {sessionId} is not active.");

        var agent = session.Agent
            ?? throw new InvalidOperationException($"Session {sessionId} has no agent configured.");

        if (!agent.IsActive)
            throw new InvalidOperationException($"Agent {agent.Id} is not active.");

        if (agent.TenantId.HasValue && agent.TenantId != tenantId)
            throw new UnauthorizedAccessException("User does not have access to this agent.");

        var provider = agent.Model?.Provider
            ?? throw new InvalidOperationException($"Agent {agent.Id} has no model configured.");

        var apiKey = await _secretStore.GetSecretAsync(provider.SecretKeyName, cancellationToken)
            ?? throw new InvalidOperationException($"API key not found for provider {provider.Name}.");

        var sessionState = await _sessionStateStore.GetSessionAsync(sessionId, authenticatedUserId, cancellationToken);

        var effectiveTopK = request.Options?.TopK ?? agent.TopK;
        var effectiveTemperature = request.Options?.Temperature ?? agent.Temperature;
        var enableMemory = request.Options?.EnableMemory ?? agent.EnableMemory;
        var enableRAG = request.Options?.EnableRAG ?? agent.EnableRAG;

        var contextMemories = new List<string>();
        if (enableRAG && sessionState?.Messages.Any() == true)
        {
            var userEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message, cancellationToken);
            var similarMemories = await _memoryRepository.SearchSimilarAsync(
                sessionId,
                userEmbedding,
                topK: effectiveTopK,
                cancellationToken);

            contextMemories = similarMemories.Select(m => m.Content).ToList();
        }

        var conversationHistory = sessionState?.Messages.Select(m => $"{m.Role}: {m.Content}").ToList() ?? new List<string>();

        var pluginTools = agent.BotType == BotType.Agent
            ? agent.Plugins.Select(p => new ToolDefinition(p.PluginName, p.DaprAppIdEndpoint)).ToList()
            : new List<ToolDefinition>();

        var context = new AgentExecutionContext
        {
            Agent = agent,
            ApiKey = apiKey,
            BaseUrl = provider.BaseUrl,
            SystemPrompt = agent.SystemInstructions,
            ConversationHistory = conversationHistory,
            ContextMemories = contextMemories,
            Temperature = effectiveTemperature,
            MaxTokens = request.Options?.MaxTokens ?? agent.MaxTokens,
            Tools = pluginTools
        };

        var aiResponse = await _agentExecutionService.ExecuteAsync(context, request.Message, cancellationToken);

        session.RecordMessage();
        await _sessionRepository.UpdateAsync(session);

        var userMessage = new ConversationMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };

        var assistantMessage = new ConversationMessage
        {
            Role = "assistant",
            Content = aiResponse,
            Timestamp = DateTime.UtcNow
        };

        await _sessionStateStore.AppendMessageAsync(sessionId, authenticatedUserId, userMessage, cancellationToken);
        await _sessionStateStore.AppendMessageAsync(sessionId, authenticatedUserId, assistantMessage, cancellationToken);

        if (enableMemory)
        {
            var userMessageEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Message, cancellationToken);
            var responseEmbedding = await _embeddingService.GenerateEmbeddingAsync(aiResponse, cancellationToken);

            await _memoryRepository.AddMemoryAsync(
                new AgentMemory(Guid.NewGuid(), sessionId, MemoryRole.User, request.Message, userMessageEmbedding),
                cancellationToken);

            await _memoryRepository.AddMemoryAsync(
                new AgentMemory(Guid.NewGuid(), sessionId, MemoryRole.Assistant, aiResponse, responseEmbedding),
                cancellationToken);
        }

        return new SendMessageResponse(
            Guid.NewGuid(),
            aiResponse,
            DateTime.UtcNow,
            null,
            sessionId);
    }

    public async Task<SessionDetailsResponse?> GetSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithAgentAsync(sessionId, cancellationToken);
        if (session == null) return null;

        if (session.UserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not own this session.");

        var sessionState = await _sessionStateStore.GetSessionAsync(sessionId, authenticatedUserId, cancellationToken);
        var messages = sessionState?.Messages ?? new List<ConversationMessage>();

        return new SessionDetailsResponse(
            session.Id,
            session.AgentId,
            session.Agent?.Name ?? "Unknown",
            session.Agent?.BotType ?? BotType.Chat,
            session.UserId,
            session.Title,
            session.StartedAt,
            session.LastMessageAt,
            session.Status,
            messages.Select(m => new SessionMessageDto(Guid.NewGuid(), m.Role, m.Content, m.Timestamp)).ToList());
    }

    public async Task<IEnumerable<SessionListItemDto>> ListSessionsAsync(
        string authenticatedUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(authenticatedUserId, cancellationToken);

        var sessionDtos = new List<SessionListItemDto>();
        foreach (var session in sessions)
        {
            var sessionState = await _sessionStateStore.GetSessionAsync(session.Id, authenticatedUserId, cancellationToken);
            var messageCount = sessionState?.Messages.Count ?? 0;

            sessionDtos.Add(new SessionListItemDto(
                session.Id,
                session.AgentId,
                session.Agent?.Name ?? "Unknown",
                session.Agent?.BotType ?? BotType.Chat,
                session.Title,
                session.StartedAt,
                session.LastMessageAt,
                session.Status,
                messageCount));
        }

        return sessionDtos;
    }

    public async Task EndSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException($"Session with ID {sessionId} not found.");

        if (session.UserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not own this session.");

        session.Complete();
        await _sessionRepository.UpdateAsync(session);
    }

    private static AgentDto MapToDto(Agent agent) => new(
        agent.Id,
        agent.Name,
        agent.Description,
        agent.ModelId,
        agent.Model?.TechnicalName ?? "N/A",
        agent.BotType,
        agent.SystemInstructions,
        agent.Temperature,
        agent.TopK,
        agent.MaxTokens,
        agent.EmbeddingDimensions,
        agent.EnableMemory,
        agent.EnableRAG,
        agent.EmbeddingModelName,
        agent.IsActive,
        agent.TenantId);

    private static AgentListDto MapToListDto(Agent agent) => new(
        agent.Id,
        agent.Name,
        agent.Description,
        agent.Model?.TechnicalName ?? "N/A",
        agent.BotType,
        agent.IsActive,
        agent.EnableMemory,
        agent.EnableRAG);
}