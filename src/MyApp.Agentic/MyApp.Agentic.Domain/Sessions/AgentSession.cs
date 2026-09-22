using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Sessions;

/// <summary>
/// Describes the lifecycle state of an <see cref="AgentSession"/>.
/// </summary>
public enum SessionStatus
{
    /// <summary>The session is open and accepting messages.</summary>
    Active,
    /// <summary>The session was closed normally by the user.</summary>
    Completed,
    /// <summary>The session timed out due to inactivity.</summary>
    Expired
}

/// <summary>
/// Agent session.
/// </summary>
/// <param name="id">The id.</param>
public class AgentSession(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the identifier of the <see cref="Agent"/> this session belongs to.</summary>
    public Guid AgentId { get; private set; }
    /// <summary>Gets the identifier of the user who started the session.</summary>
    public string UserId { get; private set; } = string.Empty;
    /// <summary>Gets the optional human-readable title for the session.</summary>
    public string? Title { get; private set; }
    /// <summary>Gets the UTC timestamp when the session was created.</summary>
    public DateTime StartedAt { get; private set; }
    /// <summary>Gets the UTC timestamp of the most recent message, if any.</summary>
    public DateTime? LastMessageAt { get; private set; }
    /// <summary>Gets the current lifecycle status of the session.</summary>
    public SessionStatus Status { get; private set; } = SessionStatus.Active;

    /// <summary>Gets the owning <see cref="Agent"/> navigation property.</summary>
    public Agent? Agent { get; private set; }

    /// <summary>
    /// Initializes a new instance of the AgentSession class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="agentId">The agent Id.</param>
    /// <param name="userId">The user Id.</param>
    /// <param name="title">The title.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> is blank.</exception>
    public AgentSession(Guid id, Guid agentId, string userId, string? title = null) : this(id)
    {
        AgentId = agentId;
        UserId = NormalizeRequired(userId, nameof(userId));
        Title = title?.Trim();
        StartedAt = DateTime.UtcNow;
        Status = SessionStatus.Active;
    }

    /// <summary>Updates <see cref="LastMessageAt"/> to the current UTC time.</summary>
    public void RecordMessage()
    {
        LastMessageAt = DateTime.UtcNow;
    }

    /// <summary>Sets the session status to <see cref="SessionStatus.Completed"/>.</summary>
    public void Complete()
    {
        Status = SessionStatus.Completed;
    }

    /// <summary>Sets the session status to <see cref="SessionStatus.Expired"/>.</summary>
    public void Expire()
    {
        Status = SessionStatus.Expired;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}