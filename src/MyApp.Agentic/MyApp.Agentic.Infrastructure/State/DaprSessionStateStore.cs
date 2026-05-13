using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Infrastructure.State;

public interface ISessionStateStore
{
    Task<SessionState?> GetSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);
    Task SaveSessionAsync(SessionState session, CancellationToken cancellationToken = default);
    Task AppendMessageAsync(Guid agentId, string userId, ConversationMessage message, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);
}

public class DaprSessionStateStore : ISessionStateStore
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprSessionStateStore> _logger;
    private const string StateStoreName = "statestore";

    public DaprSessionStateStore(DaprClient daprClient, ILogger<DaprSessionStateStore> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    private static string GetStateKey(Guid agentId, string userId) => $"agent-session:{agentId}:{userId}";

    public async Task<SessionState?> GetSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetStateKey(agentId, userId);
            var state = await _daprClient.GetStateAsync<SessionState>(StateStoreName, key, cancellationToken: cancellationToken);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get session state for Agent {AgentId}, User {UserId}", agentId, userId);
            return null;
        }
    }

    public async Task SaveSessionAsync(SessionState session, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetStateKey(session.AgentId, session.UserId);
            session.LastUpdated = DateTime.UtcNow;
            await _daprClient.SaveStateAsync(StateStoreName, key, session, cancellationToken: cancellationToken);
            _logger.LogDebug("Saved session state for Agent {AgentId}, User {UserId}", session.AgentId, session.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session state for Agent {AgentId}, User {UserId}", session.AgentId, session.UserId);
            throw;
        }
    }

    public async Task AppendMessageAsync(Guid agentId, string userId, ConversationMessage message, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(agentId, userId, cancellationToken);
        if (session == null)
        {
            session = new SessionState
            {
                SessionId = Guid.NewGuid(),
                AgentId = agentId,
                UserId = userId,
                Messages = new List<ConversationMessage>()
            };
        }

        session.Messages.Add(message);
        await SaveSessionAsync(session, cancellationToken);
    }

    public async Task DeleteSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetStateKey(agentId, userId);
            await _daprClient.DeleteStateAsync(StateStoreName, key, cancellationToken: cancellationToken);
            _logger.LogDebug("Deleted session state for Agent {AgentId}, User {UserId}", agentId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete session state for Agent {AgentId}, User {UserId}", agentId, userId);
        }
    }
}