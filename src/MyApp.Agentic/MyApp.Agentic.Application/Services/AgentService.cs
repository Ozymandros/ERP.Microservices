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
            systemInstructions: dto.SystemInstructions,
            tenantId: dto.TenantId,
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
            dto.SystemInstructions,
            dto.TopK,
            dto.MaxTokens,
            dto.EmbeddingDimensions,
            dto.EnableMemory,
            dto.EnableRAG,
            dto.EmbeddingModelName);

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

    private static AgentDto MapToDto(Agent agent) => new(
        agent.Id,
        agent.Name,
        agent.Description,
        agent.ModelId,
        agent.Model?.TechnicalName ?? "N/A",
        agent.Temperature,
        agent.TopK,
        agent.MaxTokens,
        agent.EmbeddingDimensions,
        agent.EnableMemory,
        agent.EnableRAG,
        agent.EmbeddingModelName,
        agent.SystemInstructions,
        agent.IsActive,
        agent.TenantId);

    private static AgentListDto MapToListDto(Agent agent) => new(
        agent.Id,
        agent.Name,
        agent.Description,
        agent.Model?.TechnicalName ?? "N/A",
        agent.IsActive,
        agent.EnableMemory,
        agent.EnableRAG);
}