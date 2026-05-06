using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Sessions;

public enum SessionStatus
{
    Active,
    Completed,
    Expired
}

public class AgentSession(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid AgentId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? LastMessageAt { get; private set; }
    public SessionStatus Status { get; private set; } = SessionStatus.Active;

    public Agent? Agent { get; private set; }

    public AgentSession(Guid id, Guid agentId, string userId) : this(id)
    {
        AgentId = agentId;
        UserId = NormalizeRequired(userId, nameof(userId));
        StartedAt = DateTime.UtcNow;
        Status = SessionStatus.Active;
    }

    public void RecordMessage()
    {
        LastMessageAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = SessionStatus.Completed;
    }

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