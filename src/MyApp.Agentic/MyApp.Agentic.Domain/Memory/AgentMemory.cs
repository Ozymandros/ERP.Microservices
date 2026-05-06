using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Memory;

public enum MemoryRole
{
    User,
    Assistant
}

public class AgentMemory(Guid id) : DomainEntity<Guid>(id)
{
    public Guid SessionId { get; private set; }
    public MemoryRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public float[]? Embedding { get; private set; }
    public string? Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public AgentMemory(Guid id, Guid sessionId, MemoryRole role, string content, float[]? embedding = null, string? metadata = null) : this(id)
    {
        SessionId = sessionId;
        Role = role;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Embedding = embedding;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
    }
}