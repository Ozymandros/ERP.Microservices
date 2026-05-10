using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Infrastructure.Memory;
using MyApp.Agentic.Infrastructure.State;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Security;

namespace MyApp.Agentic.Application.Tests;

public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _mockAgentRepository;
    private readonly Mock<IAIProviderRepository> _mockProviderRepository;
    private readonly Mock<IAIModelRepository> _mockModelRepository;
    private readonly Mock<IAgentSessionRepository> _mockSessionRepository;
    private readonly Mock<IMemoryRepository> _mockMemoryRepository;
    private readonly Mock<ISecretCryptoService> _mockSecretCryptoService;
    private readonly Mock<ISessionStateStore> _mockSessionStateStore;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IAgentExecutionService> _mockExecutionService;
    private readonly Mock<IServiceInvoker> _mockServiceInvoker;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AgentService>> _mockLogger;
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _mockAgentRepository = new Mock<IAgentRepository>();
        _mockProviderRepository = new Mock<IAIProviderRepository>();
        _mockModelRepository = new Mock<IAIModelRepository>();
        _mockSessionRepository = new Mock<IAgentSessionRepository>();
        _mockMemoryRepository = new Mock<IMemoryRepository>();
        _mockSecretCryptoService = new Mock<ISecretCryptoService>();
        _mockSessionStateStore = new Mock<ISessionStateStore>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockExecutionService = new Mock<IAgentExecutionService>();
        _mockServiceInvoker = new Mock<IServiceInvoker>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AgentService>>();

        _mockServiceInvoker
            .Setup(s => s.InvokeAsync<string, object>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        _mockSecretCryptoService
            .Setup(s => s.Decrypt(It.IsAny<string>()))
            .Returns("test-api-key");

        _service = new AgentService(
            _mockAgentRepository.Object,
            _mockProviderRepository.Object,
            _mockModelRepository.Object,
            _mockSessionRepository.Object,
            _mockMemoryRepository.Object,
            _mockSecretCryptoService.Object,
            _mockSessionStateStore.Object,
            _mockEmbeddingService.Object,
            _mockExecutionService.Object,
            _mockServiceInvoker.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    private Agent CreateTestAgent(Guid id = default, string? ownerUserId = null)
    {
        var provider = new AIProvider(Guid.NewGuid(), "OpenAI", "https://api.openai.com", "openai-key");
        var model = new AIModel(Guid.NewGuid(), provider.Id, "GPT-4", "gpt-4", 8192, "chat");
        model.SetProviderForTest(provider);

        var agent = new Agent(
            id == default ? Guid.NewGuid() : id,
            "Test Agent",
            "A test agent",
            model.Id,
            0.7,
            "You are a helpful assistant.",
            ownerUserId);

        agent.SetModelForTest(model);

        return agent;
    }

    [Fact]
    public async Task GetByIdAsync_WhenAgentExists_ReturnsAgentDto()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId);

        _mockAgentRepository.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);

        var result = await _service.GetByIdAsync(agentId);

        Assert.NotNull(result);
        Assert.Equal(agentId, result.Id);
        Assert.Equal("Test Agent", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAgentNotExists_ReturnsNull()
    {
        var agentId = Guid.NewGuid();
        _mockAgentRepository.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync((Agent?)null);

        var result = await _service.GetByIdAsync(agentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllAgents()
    {
        var agents = new List<Agent>
        {
            CreateTestAgent(Guid.NewGuid()),
            CreateTestAgent(Guid.NewGuid())
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);

        var result = await _service.ListAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCreatedAgent()
    {
        var dto = new CreateAgentDto(
            "New Agent",
            "Description",
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.5,
            "Instructions",
            null,
            BotType.Chat,
            5,
            1024,
            1536,
            true,
            true,
            "text-embedding-ada-002");

        _mockProviderRepository.Setup(r => r.GetByIdAsync(dto.ProviderId)).ReturnsAsync(new AIProvider(dto.ProviderId, "OpenAI", "https://api.openai.com/v1", "sk-test-key"));
        _mockModelRepository.Setup(r => r.GetByIdAsync(dto.ModelId)).ReturnsAsync(new AIModel(dto.ModelId, dto.ProviderId, "GPT-5", "gpt-5", 8192, "chat"));
        _mockAgentRepository.Setup(r => r.AddAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("New Agent", result.Name);
        Assert.Equal(0.5, result.Temperature);
        Assert.Equal(5, result.TopK);
        Assert.Equal(1024, result.MaxTokens);
        Assert.Equal(1536, result.EmbeddingDimensions);
        Assert.True(result.EnableMemory);
        Assert.True(result.EnableRAG);
    }

    [Fact]
    public async Task UpdateAsync_WhenAgentExists_UpdatesAgent()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId);
        var dto = new UpdateAgentDto(
            "Updated Name",
            "Updated Description",
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.9,
            "Updated Instructions",
            BotType.Chat,
            10,
            4096,
            2048,
            false,
            true,
            "custom-embedding");

        _mockProviderRepository.Setup(r => r.GetByIdAsync(dto.ProviderId)).ReturnsAsync(new AIProvider(dto.ProviderId, "OpenAI", "https://api.openai.com/v1", "sk-test-key"));
        _mockModelRepository.Setup(r => r.GetByIdAsync(dto.ModelId)).ReturnsAsync(new AIModel(dto.ModelId, dto.ProviderId, "GPT-5", "gpt-5", 8192, "chat"));
        _mockAgentRepository.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockAgentRepository.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _service.UpdateAsync(agentId, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(0.9, result.Temperature);
        Assert.Equal(10, result.TopK);
    }

    [Fact]
    public async Task UpdateAsync_WhenAgentNotExists_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var dto = new UpdateAgentDto("Name", "Desc", Guid.NewGuid(), Guid.NewGuid(), 0.7, "Instructions");

        _mockAgentRepository.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync((Agent?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(agentId, dto));
    }

    [Fact]
    public async Task CreateAsync_WhenModelDoesNotBelongToProvider_ThrowsException()
    {
        var providerId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var dto = new CreateAgentDto("New Agent", "Description", providerId, modelId, 0.5, "Instructions", null);

        _mockProviderRepository.Setup(r => r.GetByIdAsync(providerId))
            .ReturnsAsync(new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "sk-test-key"));
        _mockModelRepository.Setup(r => r.GetByIdAsync(modelId))
            .ReturnsAsync(new AIModel(modelId, otherProviderId, "GPT-5", "gpt-5", 8192, "chat"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenProviderIdIsEmpty_ThrowsException()
    {
        var dto = new CreateAgentDto("New Agent", "Description", Guid.Empty, Guid.NewGuid(), 0.5, "Instructions", null);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_WhenModelIdIsEmpty_ThrowsException()
    {
        var providerId = Guid.NewGuid();
        var dto = new CreateAgentDto("New Agent", "Description", providerId, Guid.Empty, 0.5, "Instructions", null);

        _mockProviderRepository.Setup(r => r.GetByIdAsync(providerId))
            .ReturnsAsync(new AIProvider(providerId, "OpenAI", "https://api.openai.com/v1", "sk-test-key"));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task DeleteAsync_WhenAgentExists_DeletesAgent()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId);

        _mockAgentRepository.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync(agent);
        _mockAgentRepository.Setup(r => r.DeleteAsync(It.IsAny<Agent>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(agentId);

        _mockAgentRepository.Verify(r => r.DeleteAsync(agent), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidRequest_ReturnsResponse()
    {
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var agent = CreateTestAgent(agentId);

        var request = new ProcessAgentMessageRequest(agentId, "Hello", new AgentExecutionOptions(0.8));

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(agentId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        _mockEmbeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello! How can I help you?");

        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ProcessMessageAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Hello", result.UserMessage);
        Assert.NotNull(result.AIResponse);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenAgentNotActive_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var agent = CreateTestAgent(agentId);
        agent.Deactivate();

        var request = new ProcessAgentMessageRequest(agentId, "Hello");

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ProcessMessageAsync(request, userId));
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenAgentOwnerMismatch_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var ownerUserId = "owner-user";
        var agent = CreateTestAgent(agentId, ownerUserId);

        var request = new ProcessAgentMessageRequest(agentId, "Hello");

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ProcessMessageAsync(request, "different-user"));
    }

    [Fact]
    public async Task ProcessMessageAsync_WithRAGEnabled_RetrievesContextMemories()
    {
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var sessionId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId);

        var existingSession = new SessionState
        {
            SessionId = sessionId,
            AgentId = agentId,
            UserId = userId,
            Messages = new List<ConversationMessage>
            {
                new() { Role = "user", Content = "Previous message", Timestamp = DateTime.UtcNow }
            }
        };

        var request = new ProcessAgentMessageRequest(agentId, "Hello", new AgentExecutionOptions(null, null, null, null, true));

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(agentId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        _mockEmbeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        var memories = new List<AgentMemory>
        {
            new(Guid.NewGuid(), sessionId, MemoryRole.User, "Previous context", new float[1536])
        };

        _mockMemoryRepository.Setup(r => r.SearchSimilarAsync(sessionId, It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(memories);

        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response with context");

        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ProcessMessageAsync(request, userId);

        _mockExecutionService.Verify(e => e.ExecuteAsync(
            It.Is<AgentExecutionContext>(ctx => ctx.ContextMemories.Count > 0),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithMemoryDisabled_DoesNotStoreMemory()
    {
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var agent = CreateTestAgent(agentId);

        var request = new ProcessAgentMessageRequest(agentId, "Hello", new AgentExecutionOptions(null, null, null, false));

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(agentId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response");

        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.ProcessMessageAsync(request, userId);

        _mockMemoryRepository.Verify(r => r.AddMemoryAsync(It.IsAny<AgentMemory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListByOwnerAsync_FiltersByOwner()
    {
        var ownerUserId = "owner-user";
        var agents = new List<Agent>
        {
            CreateTestAgent(Guid.NewGuid(), ownerUserId),
            CreateTestAgent(Guid.NewGuid(), ownerUserId),
            CreateTestAgent(Guid.NewGuid(), null)
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);

        var result = await _service.ListByOwnerAsync(ownerUserId);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task ListByOwnerAsync_WithoutOwner_ReturnsAll()
    {
        var agents = new List<Agent>
        {
            CreateTestAgent(Guid.NewGuid(), "another-owner"),
            CreateTestAgent(Guid.NewGuid(), null)
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);

        var result = await _service.ListByOwnerAsync(null);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task StartSessionAsync_WithValidRequest_ReturnsSession()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId);
        var userId = "user-123";
        var request = new StartSessionRequest(agentId, userId, "Test Session");

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        _mockSessionRepository.Setup(r => r.AddAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        var result = await _service.StartSessionAsync(request, userId);

        Assert.NotNull(result);
        Assert.Equal(agentId, result.AgentId);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task StartSessionAsync_WhenAgentNotFound_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var request = new StartSessionRequest(agentId, userId, "Test");

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Agent?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartSessionAsync(request, "user-123"));
    }

    [Fact]
    public async Task StartSessionAsync_WhenAgentNotActive_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId);
        agent.Deactivate();
        var userId = "user-123";
        var request = new StartSessionRequest(agentId, userId, "Test");

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartSessionAsync(request, "user-123"));
    }

    [Fact]
    public async Task SendMessageAsync_WithValidSession_ReturnsResponse()
    {
        var sessionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var agent = CreateTestAgent(agentId);
        var session = new AgentSession(sessionId, agentId, userId, "Test Session");
        
        typeof(AgentSession).GetProperty("Agent")?.SetValue(session, agent);

        var request = new SendMessageRequest("Hello", new ProcessMessageOptions(0.8));

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(sessionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response");

        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.SendMessageAsync(sessionId, request, userId);

        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
    }

    [Fact]
    public async Task SendMessageAsync_WithChatBotType_IncludesOnlyGetTools()
    {
        var sessionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var userId = "user-123";

        var provider = new AIProvider(Guid.NewGuid(), "OpenAI", "https://api.openai.com", "openai-key");
        var model = new AIModel(Guid.NewGuid(), provider.Id, "GPT-4", "gpt-4", 8192, "chat");
        model.SetProviderForTest(provider);

        var agent = new Agent(
            agentId,
            "Chat Agent",
            "Chat-only agent",
            model.Id,
            0.7,
            "You are a helpful assistant.",
            botType: BotType.Chat);

        agent.SetModelForTest(model);
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "GetByIdAsync", "inventory.getById"));
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "CreateAsync", "inventory.create"));

        var session = new AgentSession(sessionId, agentId, userId, "Test Session");
        typeof(AgentSession).GetProperty("Agent")?.SetValue(session, agent);

        var request = new SendMessageRequest("Hello", null);
        AgentExecutionContext? capturedContext = null;

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockSessionStateStore.Setup(s => s.GetSessionAsync(sessionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);
        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionContext, string, CancellationToken>((ctx, _, _) => capturedContext = ctx)
            .ReturnsAsync("Response");
        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.SendMessageAsync(sessionId, request, userId);

        Assert.NotNull(capturedContext);
        Assert.Single(capturedContext!.Tools);
        Assert.Equal("GetByIdAsync", capturedContext.Tools[0].Name);
        Assert.Equal(ToolHttpVerb.Get, capturedContext.Tools[0].Verb);
    }

    [Fact]
    public async Task SendMessageAsync_WithAgentBotType_IncludesAllToolVerbs()
    {
        var sessionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var userId = "user-123";

        var provider = new AIProvider(Guid.NewGuid(), "OpenAI", "https://api.openai.com", "openai-key");
        var model = new AIModel(Guid.NewGuid(), provider.Id, "GPT-4", "gpt-4", 8192, "chat");
        model.SetProviderForTest(provider);

        var agent = new Agent(
            agentId,
            "Agent Mode",
            "Full tools agent",
            model.Id,
            0.7,
            "You are a helpful assistant.",
            botType: BotType.Agent);

        agent.SetModelForTest(model);
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "GetByIdAsync", "inventory.getById"));
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "CreateAsync", "inventory.create"));
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "UpdateAsync", "inventory.update"));
        agent.Plugins.Add(new AgentPlugin(Guid.NewGuid(), agent.Id, "DeleteAsync", "inventory.delete"));

        var session = new AgentSession(sessionId, agentId, userId, "Test Session");
        typeof(AgentSession).GetProperty("Agent")?.SetValue(session, agent);

        var request = new SendMessageRequest("Hello", null);
        AgentExecutionContext? capturedContext = null;

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _mockSessionStateStore.Setup(s => s.GetSessionAsync(sessionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);
        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AgentExecutionContext, string, CancellationToken>((ctx, _, _) => capturedContext = ctx)
            .ReturnsAsync("Response");
        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.SendMessageAsync(sessionId, request, userId);

        Assert.NotNull(capturedContext);
        Assert.Equal(4, capturedContext!.Tools.Count);
        Assert.Contains(capturedContext.Tools, t => t.Verb == ToolHttpVerb.Get);
        Assert.Contains(capturedContext.Tools, t => t.Verb == ToolHttpVerb.Post);
        Assert.Contains(capturedContext.Tools, t => t.Verb == ToolHttpVerb.Put);
        Assert.Contains(capturedContext.Tools, t => t.Verb == ToolHttpVerb.Delete);
    }

    [Fact]
    public async Task SendMessageAsync_WhenSessionNotFound_ThrowsException()
    {
        var sessionId = Guid.NewGuid();
        var request = new SendMessageRequest("Hello", null);

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentSession?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendMessageAsync(sessionId, request, "user-123"));
    }

    [Fact]
    public async Task SendMessageAsync_WhenUserNotOwner_ThrowsException()
    {
        var sessionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var session = new AgentSession(sessionId, agentId, "other-user", "Test");
        var request = new SendMessageRequest("Hello", null);

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.SendMessageAsync(sessionId, request, "user-123"));
    }

    [Fact]
    public async Task GetSessionAsync_WhenSessionExists_ReturnsDetails()
    {
        var sessionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var userId = "user-123";
        var agent = CreateTestAgent(agentId);
        var session = new AgentSession(sessionId, agentId, userId, "Test Session");
        
        typeof(AgentSession).GetProperty("Agent")?.SetValue(session, agent);

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(sessionId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        var result = await _service.GetSessionAsync(sessionId, userId);

        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
    }

    [Fact]
    public async Task GetSessionAsync_WhenSessionNotExists_ReturnsNull()
    {
        var sessionId = Guid.NewGuid();

        _mockSessionRepository.Setup(r => r.GetByIdWithAgentAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentSession?)null);

        var result = await _service.GetSessionAsync(sessionId, "user-123");

        Assert.Null(result);
    }

    [Fact]
    public async Task ListSessionsAsync_ReturnsUserSessions()
    {
        var userId = "user-123";
        var sessions = new List<AgentSession>
        {
            new AgentSession(Guid.NewGuid(), Guid.NewGuid(), userId, "Session 1"),
            new AgentSession(Guid.NewGuid(), Guid.NewGuid(), userId, "Session 2")
        };

        _mockSessionRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        var result = await _service.ListSessionsAsync(userId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task EndSessionAsync_MarksSessionCompleted()
    {
        var sessionId = Guid.NewGuid();
        var userId = "user-123";
        var session = new AgentSession(sessionId, Guid.NewGuid(), userId);

        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockSessionRepository.Setup(r => r.UpdateAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        await _service.EndSessionAsync(sessionId, userId);

        _mockSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<AgentSession>()), Times.Once);
    }

    [Fact]
    public async Task EndSessionAsync_WhenNotOwner_ThrowsException()
    {
        var sessionId = Guid.NewGuid();
        var session = new AgentSession(sessionId, Guid.NewGuid(), "other-user");

        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.EndSessionAsync(sessionId, "user-123"));
    }
}

public static class AgentTestHelpers
{
    public static void SetProviderForTest(this AIModel model, AIProvider provider)
    {
        var providerField = typeof(AIModel).GetProperty("Provider");
        if (providerField != null)
        {
            providerField.SetValue(model, provider);
        }
    }

    public static void SetModelForTest(this Agent agent, AIModel model)
    {
        var modelField = typeof(Agent).GetProperty("Model");
        if (modelField != null)
        {
            modelField.SetValue(agent, model);
        }
    }
}
