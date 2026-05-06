using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Infrastructure.Memory;
using MyApp.Agentic.Infrastructure.Secrets;
using MyApp.Agentic.Infrastructure.State;

namespace MyApp.Agentic.Application.Tests;

public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _mockAgentRepository;
    private readonly Mock<IAgentSessionRepository> _mockSessionRepository;
    private readonly Mock<IMemoryRepository> _mockMemoryRepository;
    private readonly Mock<ISecretStore> _mockSecretStore;
    private readonly Mock<ISessionStateStore> _mockSessionStateStore;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IAgentExecutionService> _mockExecutionService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AgentService>> _mockLogger;
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _mockAgentRepository = new Mock<IAgentRepository>();
        _mockSessionRepository = new Mock<IAgentSessionRepository>();
        _mockMemoryRepository = new Mock<IMemoryRepository>();
        _mockSecretStore = new Mock<ISecretStore>();
        _mockSessionStateStore = new Mock<ISessionStateStore>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockExecutionService = new Mock<IAgentExecutionService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AgentService>>();

        _service = new AgentService(
            _mockAgentRepository.Object,
            _mockSessionRepository.Object,
            _mockMemoryRepository.Object,
            _mockSecretStore.Object,
            _mockSessionStateStore.Object,
            _mockEmbeddingService.Object,
            _mockExecutionService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    private Agent CreateTestAgent(Guid id = default, Guid? tenantId = null)
    {
        var provider = new AIProvider(Guid.NewGuid(), "OpenAI", "https://api.openai.com", "openai-key");
        var model = new AIModel(Guid.NewGuid(), provider.Id, "gpt-4", 8192, "chat");
        model.SetProviderForTest(provider);

        var agent = new Agent(
            id == default ? Guid.NewGuid() : id,
            "Test Agent",
            "A test agent",
            model.Id,
            0.7,
            "You are a helpful assistant.",
            tenantId);

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
            0.9,
            "Updated Instructions",
            BotType.Chat,
            10,
            4096,
            2048,
            false,
            true,
            "custom-embedding");

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
        var dto = new UpdateAgentDto("Name", "Desc", Guid.NewGuid(), 0.7, "Instructions");

        _mockAgentRepository.Setup(r => r.GetByIdAsync(agentId)).ReturnsAsync((Agent?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(agentId, dto));
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

        _mockSecretStore.Setup(s => s.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-api-key");

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(agentId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        _mockEmbeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello! How can I help you?");

        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ProcessMessageAsync(request, userId, null);

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
            _service.ProcessMessageAsync(request, userId, null));
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenAgentTenantMismatch_ThrowsException()
    {
        var agentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var agent = CreateTestAgent(agentId, tenantId);

        var request = new ProcessAgentMessageRequest(agentId, "Hello");

        _mockAgentRepository.Setup(r => r.GetByIdWithDetailsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ProcessMessageAsync(request, "user-123", Guid.NewGuid()));
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

        _mockSecretStore.Setup(s => s.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-api-key");

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

        var result = await _service.ProcessMessageAsync(request, userId, null);

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

        _mockSecretStore.Setup(s => s.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-api-key");

        _mockSessionStateStore.Setup(s => s.GetSessionAsync(agentId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionState?)null);

        _mockExecutionService.Setup(e => e.ExecuteAsync(It.IsAny<AgentExecutionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response");

        _mockSessionStateStore.Setup(s => s.AppendMessageAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ConversationMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.ProcessMessageAsync(request, userId, null);

        _mockMemoryRepository.Verify(r => r.AddMemoryAsync(It.IsAny<AgentMemory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListByTenantAsync_FiltersByTenant()
    {
        var tenantId = Guid.NewGuid();
        var agents = new List<Agent>
        {
            CreateTestAgent(Guid.NewGuid(), tenantId),
            CreateTestAgent(Guid.NewGuid(), tenantId),
            CreateTestAgent(Guid.NewGuid(), null)
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);

        var result = await _service.ListByTenantAsync(tenantId);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task ListByTenantAsync_WithoutTenant_ReturnsAll()
    {
        var agents = new List<Agent>
        {
            CreateTestAgent(Guid.NewGuid(), Guid.NewGuid()),
            CreateTestAgent(Guid.NewGuid(), null)
        };

        _mockAgentRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);

        var result = await _service.ListByTenantAsync(null);

        Assert.Equal(2, result.Count());
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