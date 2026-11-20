using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Infrastructure.State;

/// <summary>Provides persistent storage for agent conversation state keyed by agent and user identifiers.</summary>
public interface ISessionStateStore
{
    /// <summary>Retrieves the conversation session for the specified agent–user pair.</summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session state, or <see langword="null"/> if no session exists or retrieval fails.</returns>
    Task<SessionState?> GetSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Persists the full session state document.</summary>
    /// <param name="session">Session state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveSessionAsync(SessionState session, CancellationToken cancellationToken = default);

    /// <summary>Appends a message to the session, creating the session if it does not yet exist.</summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="message">Message to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AppendMessageAsync(Guid agentId, string userId, ConversationMessage message, CancellationToken cancellationToken = default);

    /// <summary>Removes the session state for the specified agent–user pair.</summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteSessionAsync(Guid agentId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>Dapr state-store backed implementation of <see cref="ISessionStateStore"/>.</summary>
public class DaprSessionStateStore : ISessionStateStore
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprSessionStateStore> _logger;
    private const string StateStoreName = "statestore";

    /// <summary>Initializes a new instance of the <see cref="DaprSessionStateStore"/> class.</summary>
    /// <param name="daprClient">Dapr client used for state-store operations.</param>
    /// <param name="logger">Structured logger.</param>
    public DaprSessionStateStore(DaprClient daprClient, ILogger<DaprSessionStateStore> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    private static string GetStateKey(Guid agentId, string userId) => $"agent-session:{agentId}:{userId}";

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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