using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Domain.Skills;
using MyApp.Agentic.Infrastructure.Memory;
using MyApp.Agentic.Infrastructure.State;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Security;

namespace MyApp.Agentic.Application.Services;

/// <summary>
/// Coordinates agent lifecycle operations, conversation execution, session management,
/// retrieval-augmented context loading, and long-term memory persistence.
/// </summary>
/// <remarks>
/// This service orchestrates multiple infrastructure and domain components:
/// repositories, secret resolution, session state, embeddings, tool mapping, and AI execution.
/// </remarks>
public class AgentService : AppServiceBase, IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAIProviderRepository _providerRepository;
    private readonly IAIModelRepository _modelRepository;
    private readonly IAgentSessionRepository _sessionRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly ISecretCryptoService _secretCryptoService;
    private readonly ISessionStateStore _sessionStateStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly IAgentExecutionService _agentExecutionService;
    private readonly IAgentToolResolver _toolResolver;
    private readonly IServiceInvoker _serviceInvoker;
    private readonly IMapper _mapper;
    private readonly ILogger<AgentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentService"/> class.
    /// </summary>
    /// <param name="agentRepository">Repository for agent aggregate persistence.</param>
    /// <param name="providerRepository">Repository for AI provider metadata.</param>
    /// <param name="modelRepository">Repository for AI model metadata.</param>
    /// <param name="sessionRepository">Repository for persisted agent sessions.</param>
    /// <param name="memoryRepository">Repository for vectorized conversational memories.</param>
    /// <param name="secretCryptoService">Service used to decrypt provider API keys for runtime execution.</param>
    /// <param name="sessionStateStore">Transient/operational store for conversation state.</param>
    /// <param name="embeddingService">Service used to generate embeddings for RAG and memory.</param>
    /// <param name="agentExecutionService">Service that executes prompts against the configured model/provider.</param>
    /// <param name="toolResolver">Resolves ERP plugin tools available to an agent at runtime.</param>
    /// <param name="unitOfWork">Unit of work for transactional persistence.</param>
    /// <param name="eventPublisher">Publisher for domain integration events.</param>
    /// <param name="serviceInvoker">Cross-service invoker for validating external dependencies (for example auth users).</param>
    /// <param name="mapper">Object mapper dependency.</param>
    /// <param name="logger">Structured logger for diagnostics and operational tracing.</param>
    public AgentService(
        IAgentRepository agentRepository,
        IAIProviderRepository providerRepository,
        IAIModelRepository modelRepository,
        IAgentSessionRepository sessionRepository,
        IMemoryRepository memoryRepository,
        ISecretCryptoService secretCryptoService,
        ISessionStateStore sessionStateStore,
        IEmbeddingService embeddingService,
        IAgentExecutionService agentExecutionService,
        IAgentToolResolver toolResolver,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IServiceInvoker serviceInvoker,
        IMapper mapper,
        ILogger<AgentService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Agentic)
    {
        _agentRepository = agentRepository;
        _providerRepository = providerRepository;
        _modelRepository = modelRepository;
        _sessionRepository = sessionRepository;
        _memoryRepository = memoryRepository;
        _secretCryptoService = secretCryptoService;
        _sessionStateStore = sessionStateStore;
        _embeddingService = embeddingService;
        _agentExecutionService = agentExecutionService;
        _toolResolver = toolResolver;
        _serviceInvoker = serviceInvoker;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets a single agent by identifier.
    /// </summary>
    /// <param name="id">Agent identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped <see cref="AgentDto"/> when found; otherwise <see langword="null"/>.</returns>
    public async Task<AgentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        return agent == null ? null : MapToDto(agent);
    }

    /// <summary>
    /// Lists all agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of lightweight agent list DTOs.</returns>
    public async Task<IEnumerable<AgentListDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _agentRepository.GetAllAsync();
        return agents.Select(MapToListDto);
    }

    /// <summary>
    /// Lists agents visible to a given owner context.
    /// </summary>
    /// <param name="ownerUserId">
    /// Owner user identifier. When null/empty, all agents are returned; otherwise owned and shared (owner null) agents are returned.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered collection of <see cref="AgentListDto"/>.</returns>
    public async Task<IEnumerable<AgentListDto>> ListByOwnerAsync(string? ownerUserId, CancellationToken cancellationToken = default)
    {
        var agents = await _agentRepository.GetAllAsync();
        var filtered = string.IsNullOrWhiteSpace(ownerUserId)
            ? agents
            : agents.Where(a => a.OwnerUserId == ownerUserId || a.OwnerUserId == null);
        return filtered.Select(MapToListDto);
    }

    /// <summary>
    /// Creates a new agent after validating provider/model consistency and optional owner existence.
    /// </summary>
    /// <param name="dto">Agent creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created agent as <see cref="AgentDto"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when required identifiers are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when provider/model/user validation fails.</exception>
    public async Task<AgentDto> CreateAsync(CreateAgentDto dto, CancellationToken cancellationToken = default)
    {
        var model = await ValidateProviderModelAsync(dto.ProviderId, dto.ModelId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(dto.OwnerUserId))
        {
            await ValidateUserExistsAsync(dto.OwnerUserId!, cancellationToken);
        }

        var agent = new Agent(
            id: Guid.NewGuid(),
            name: dto.Name,
            description: dto.Description,
            modelId: dto.ModelId,
            temperature: dto.Temperature,
            systemInstructions: dto.SystemPrompt,
            ownerUserId: dto.OwnerUserId,
            botType: dto.BotType,
            topK: dto.TopK,
            maxTokens: dto.MaxTokens,
            embeddingDimensions: dto.EmbeddingDimensions,
            enableMemory: dto.EnableMemory,
            enableRAG: dto.EnableRAG,
            embeddingModelName: dto.EmbeddingModelName);

        agent.SetModel(model);

        await _agentRepository.AddAsync(agent);
        await SaveChangesAsync(cancellationToken);
        return MapToDto(agent);
    }

    /// <summary>
    /// Updates an existing agent configuration.
    /// </summary>
    /// <param name="id">Agent identifier.</param>
    /// <param name="dto">Update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated agent as <see cref="AgentDto"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the agent, provider, or model is invalid/not found.</exception>
    public async Task<AgentDto> UpdateAsync(Guid id, UpdateAgentDto dto, CancellationToken cancellationToken = default)
    {
        var model = await ValidateProviderModelAsync(dto.ProviderId, dto.ModelId, cancellationToken);

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

        agent.SetModel(model);

        await _agentRepository.UpdateAsync(agent);
        await SaveChangesAsync(cancellationToken);
        return MapToDto(agent);
    }

    /// <summary>
    /// Deletes an agent when it exists. No-op if the agent is not found.
    /// </summary>
    /// <param name="id">Agent identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null) return;
        await _agentRepository.DeleteAsync(agent);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Processes a direct agent message (agent-scoped session state path), executes AI response generation,
    /// appends conversation state, and optionally stores vector memories.
    /// </summary>
    /// <param name="request">Message request and runtime options.</param>
    /// <param name="authenticatedUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response containing session information and assistant output.</returns>
    /// <exception cref="InvalidOperationException">Thrown when agent/provider/configuration prerequisites are invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user access to the agent is not allowed.</exception>
    public async Task<ProcessAgentMessageResponse> ProcessMessageAsync(
        ProcessAgentMessageRequest request,
        string authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing message for Agent {AgentId}, User {UserId}", request.AgentId, authenticatedUserId);

        var agent = await _agentRepository.GetByIdWithDetailsAsync(request.AgentId, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with ID {request.AgentId} not found.");

        if (!agent.IsActive)
            throw new InvalidOperationException($"Agent {request.AgentId} is not active.");

        if (!string.IsNullOrWhiteSpace(agent.OwnerUserId) && agent.OwnerUserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not have access to this agent.");

        var provider = agent.Model?.Provider
            ?? throw new InvalidOperationException($"Agent {request.AgentId} has no model configured.");

        var encryptedApiKey = provider.EncryptedApiKey;
        if (string.IsNullOrWhiteSpace(encryptedApiKey))
            throw new InvalidOperationException($"API key not configured for provider {provider.Name}.");

        var apiKey = _secretCryptoService.Decrypt(encryptedApiKey);
        var embeddingProvider = CreateEmbeddingProviderContext(provider, agent.Model, apiKey);

        var session = await _sessionRepository.GetActiveSessionAsync(request.AgentId, authenticatedUserId, cancellationToken);
        if (session is null)
        {
            session = new AgentSession(Guid.NewGuid(), request.AgentId, authenticatedUserId);
            await _sessionRepository.AddAsync(session);
        }

        var sessionId = session.Id;
        var sessionMessages = await _memoryRepository.GetMessagesAsync(sessionId, cancellationToken);

        var effectiveTopK = request.Options?.TopK ?? agent.TopK;
        var effectiveTemperature = request.Options?.Temperature ?? agent.Temperature;
        var enableMemory = request.Options?.EnableMemory ?? agent.EnableMemory;
        var enableRAG = request.Options?.EnableRAG ?? agent.EnableRAG;

        var contextMemories = new List<string>();
        if (enableRAG && sessionMessages.Count > 0)
        {
            var similarMemories = await _memoryRepository.SearchSimilarAsync(
                sessionId,
                request.Message,
                embeddingProvider,
                topK: effectiveTopK,
                cancellationToken);

            contextMemories = similarMemories
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => m.Content!)
                .ToList();
        }

        var conversationHistory = sessionMessages
            .Select(m => $"{m.Role.ToString().ToLowerInvariant()}: {m.Content}")
            .ToList();

        var pluginTools = _toolResolver.ResolveTools(agent);

        var context = new AgentExecutionContext
        {
            Agent = agent,
            ApiKey = apiKey,
            BaseUrl = provider.BaseUrl,
            SystemPrompt = ErpAgentSystemPrompt.Compose(agent.SystemInstructions),
            ConversationHistory = conversationHistory,
            ContextMemories = contextMemories,
            Temperature = effectiveTemperature,
            MaxTokens = request.Options?.MaxTokens ?? agent.MaxTokens,
            Tools = pluginTools.ToList()
        };

        var executionResult = await _agentExecutionService.ExecuteAsync(context, request.Message, cancellationToken);
        var aiResponse = executionResult.Content;

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

        await _memoryRepository.AddMemoriesAsync(
            [
                new AgentMemory(Guid.NewGuid(), sessionId, MemoryRole.User, request.Message),
                new AgentMemory(Guid.NewGuid(), sessionId, MemoryRole.Assistant, aiResponse)
            ],
            embeddingProvider,
            generateEmbeddings: enableMemory,
            cancellationToken: cancellationToken);

        session.RecordMessage();
        await _sessionRepository.UpdateAsync(session);
        await SaveChangesAsync(cancellationToken);

        return new ProcessAgentMessageResponse(sessionId, authenticatedUserId, request.Message, aiResponse, DateTime.UtcNow, executionResult.ToolCalls);
    }

    /// <summary>
    /// Starts a persisted chat session for an agent and user.
    /// </summary>
    /// <param name="request">Session start request.</param>
    /// <param name="authenticatedUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session descriptor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when request data or agent state is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user access to the agent is not allowed.</exception>
    public async Task<StartSessionResponse> StartSessionAsync(
        StartSessionRequest request,
        string authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        if (!request.AgentId.HasValue || request.AgentId.Value == Guid.Empty)
            throw new InvalidOperationException("AgentId is required.");

        await ValidateUserExistsAsync(authenticatedUserId, cancellationToken);

        var agentId = request.AgentId.Value;
        var agent = await _agentRepository.GetByIdWithDetailsAsync(agentId, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with ID {agentId} not found.");

        if (!agent.IsActive)
            throw new InvalidOperationException($"Agent {request.AgentId} is not active.");

        if (!string.IsNullOrWhiteSpace(agent.OwnerUserId) && agent.OwnerUserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not have access to this agent.");

        var session = new AgentSession(
            id: Guid.NewGuid(),
            agentId: agent.Id,
            userId: authenticatedUserId,
            title: request.Title);

        await _sessionRepository.AddAsync(session);
        await SaveChangesAsync(cancellationToken);

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

    /// <summary>
    /// Sends a message to an existing session, executes the agent with optional tool definitions,
    /// persists activity timestamps, and optionally writes vector memories.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="request">Message request and runtime options.</param>
    /// <param name="authenticatedUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Assistant response payload associated with the session.</returns>
    /// <exception cref="InvalidOperationException">Thrown when session or agent state is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the session/agent is not owned or accessible by the user.</exception>
    public async Task<SendMessageResponse> SendMessageAsync(
        Guid sessionId,
        SendMessageRequest request,
        string authenticatedUserId,
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

        if (!string.IsNullOrWhiteSpace(agent.OwnerUserId) && agent.OwnerUserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not have access to this agent.");

        var provider = agent.Model?.Provider
            ?? throw new InvalidOperationException($"Agent {agent.Id} has no model configured.");

        var encryptedApiKey = provider.EncryptedApiKey;
        if (string.IsNullOrWhiteSpace(encryptedApiKey))
            throw new InvalidOperationException($"API key not configured for provider {provider.Name}.");

        var apiKey = _secretCryptoService.Decrypt(encryptedApiKey);
        var embeddingProvider = CreateEmbeddingProviderContext(provider, agent.Model, apiKey);

        var sessionMessages = await _memoryRepository.GetMessagesAsync(sessionId, cancellationToken);

        var effectiveTopK = request.Options?.TopK ?? agent.TopK;
        var effectiveTemperature = request.Options?.Temperature ?? agent.Temperature;
        var enableMemory = request.Options?.EnableMemory ?? agent.EnableMemory;
        var enableRAG = request.Options?.EnableRAG ?? agent.EnableRAG;

        var contextMemories = new List<string>();
        if (enableRAG && sessionMessages.Count > 0)
        {
            var similarMemories = await _memoryRepository.SearchSimilarAsync(
                sessionId,
                request.Message,
                embeddingProvider,
                topK: effectiveTopK,
                cancellationToken);

            contextMemories = similarMemories
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => m.Content!)
                .ToList();
        }

        var conversationHistory = sessionMessages
            .Select(m => $"{m.Role.ToString().ToLowerInvariant()}: {m.Content}")
            .ToList();

        var pluginTools = _toolResolver.ResolveTools(agent);

        var context = new AgentExecutionContext
        {
            Agent = agent,
            ApiKey = apiKey,
            BaseUrl = provider.BaseUrl,
            SystemPrompt = ErpAgentSystemPrompt.Compose(agent.SystemInstructions),
            ConversationHistory = conversationHistory,
            ContextMemories = contextMemories,
            Temperature = effectiveTemperature,
            MaxTokens = request.Options?.MaxTokens ?? agent.MaxTokens,
            Tools = pluginTools.ToList()
        };

        var executionResult = await _agentExecutionService.ExecuteAsync(context, request.Message, cancellationToken);
        var aiResponse = executionResult.Content;

        session.RecordMessage();
        await _sessionRepository.UpdateAsync(session);
        await SaveChangesAsync(cancellationToken);

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

        await _memoryRepository.AddMemoriesAsync(
            [
                new AgentMemory(Guid.NewGuid(), sessionId, MemoryRole.User, request.Message),
                new AgentMemory(Guid.NewGuid(), sessionId, MemoryRole.Assistant, aiResponse)
            ],
            embeddingProvider,
            generateEmbeddings: enableMemory,
            cancellationToken: cancellationToken);

        return new SendMessageResponse(
            Guid.NewGuid(),
            aiResponse,
            DateTime.UtcNow,
            executionResult.ToolCalls,
            sessionId);
    }

    /// <summary>
    /// Gets full session details, including message history from session state storage.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="authenticatedUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session details when found; otherwise <see langword="null"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user does not own the session.</exception>
    public async Task<SessionDetailsResponse?> GetSessionAsync(
        Guid sessionId,
        string authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdWithAgentAsync(sessionId, cancellationToken);
        if (session == null) return null;

        if (session.UserId != authenticatedUserId)
            throw new UnauthorizedAccessException("User does not own this session.");

        var messages = await _memoryRepository.GetMessagesAsync(sessionId, cancellationToken);

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
            messages.Select(m => new SessionMessageDto(m.Id, m.Role.ToString().ToLowerInvariant(), m.Content ?? string.Empty, m.CreatedAt)).ToList());
    }

    /// <summary>
    /// Lists sessions for the authenticated user and enriches each with message count from state storage.
    /// </summary>
    /// <param name="authenticatedUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User session list items with summary metadata.</returns>
    public async Task<IEnumerable<SessionListItemDto>> ListSessionsAsync(
        string authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(authenticatedUserId, cancellationToken);

        var sessionDtos = new List<SessionListItemDto>();
        foreach (var session in sessions)
        {
            var messageCount = (await _memoryRepository.GetMessagesAsync(session.Id, cancellationToken)).Count;

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

    /// <summary>
    /// Marks an active session as completed.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="authenticatedUserId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when session is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when user does not own the session.</exception>
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
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Maps the domain <see cref="Agent"/> to a detailed API DTO.
    /// </summary>
    /// <param name="agent">Source domain entity.</param>
    /// <returns>Mapped <see cref="AgentDto"/>.</returns>
    private static AgentDto MapToDto(Agent agent) => new(
        agent.Id,
        agent.Name,
        agent.Description,
        agent.Model?.ProviderId ?? Guid.Empty,
        agent.Model?.Provider?.Name ?? "N/A",
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
        agent.OwnerUserId);

    /// <summary>
    /// Validates that provider and model identifiers are present, exist, and are compatible.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="modelId">Model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when an identifier is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when entities do not exist or mismatch.</exception>
    private async Task<AIModel> ValidateProviderModelAsync(Guid providerId, Guid modelId, CancellationToken cancellationToken)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));

        if (modelId == Guid.Empty)
            throw new ArgumentException("ModelId is required.", nameof(modelId));

        var provider = await _providerRepository.GetByIdAsync(providerId);
        if (provider is null)
            throw new InvalidOperationException($"AI provider with ID {providerId} was not found.");

        var model = await _modelRepository.GetByIdAsync(modelId);
        if (model is null)
            throw new InvalidOperationException($"AI model with ID {modelId} was not found.");

        if (model.ProviderId != providerId)
            throw new InvalidOperationException("Selected model does not belong to the selected provider.");

        return model;
    }

    /// <summary>
    /// Validates user existence by querying the auth service.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when user identifier is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when user cannot be found or validation fails.</exception>
    private async Task ValidateUserExistsAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        try
        {
            using var request = _serviceInvoker.CreateRequest(
                ServiceNames.Auth,
                $"api/users/{userId}",
                HttpMethod.Get);

            var user = await _serviceInvoker.InvokeAsync<object>(request, cancellationToken);

            //var user = await _serviceInvoker.GetAsync<string, object>(
            //    ServiceNames.Auth,
            //    $"api/users/{userId}",
            //    string.Empty,
            //    cancellationToken);

            if (user is null)
                throw new InvalidOperationException($"User {userId} not found in auth-service.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate user {UserId} via auth-service", userId);
            throw new InvalidOperationException($"User {userId} could not be validated against auth-service.", ex);
        }
    }

    /// <summary>
    /// Builds embedding API settings from the agent's selected model and its provider.
    /// </summary>
    /// <param name="provider">AI provider configured on the agent's model.</param>
    /// <param name="model">Selected AI model, if available.</param>
    /// <param name="apiKey">Decrypted provider API key.</param>
    /// <returns>Provider context used for memory embedding generation.</returns>
    private static MemoryEmbeddingProviderContext CreateEmbeddingProviderContext(
        AIProvider provider,
        AIModel? model,
        string apiKey)
    {
        const string defaultEmbeddingModel = "text-embedding-3-small";

        var embeddingModel = !string.IsNullOrWhiteSpace(model?.DefaultEmbeddingModelName)
            ? model.DefaultEmbeddingModelName!.Trim()
            : !string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModelName)
                ? provider.DefaultEmbeddingModelName!.Trim()
                : defaultEmbeddingModel;

        return new MemoryEmbeddingProviderContext(
            apiKey,
            provider.BaseUrl ?? string.Empty,
            embeddingModel);
    }

    /// <summary>
    /// Maps the domain <see cref="Agent"/> to a lightweight listing DTO.
    /// </summary>
    /// <param name="agent">Source domain entity.</param>
    /// <returns>Mapped <see cref="AgentListDto"/>.</returns>
    private static AgentListDto MapToListDto(Agent agent) => new(
        agent.Id,
        agent.Name,
        agent.Description,
        agent.Model?.CommercialName ?? agent.Model?.TechnicalName ?? "N/A",
        agent.BotType,
        agent.IsActive,
        agent.EnableMemory,
        agent.EnableRAG);
}
