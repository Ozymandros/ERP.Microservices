using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Memory;

public enum MemoryRole
{
    User,
    Assistant
}

/// <summary>
/// Represents a conversation memory entry with vector embedding for similarity search.
/// Uses ReadOnlyMemory of float for the embedding to align with Microsoft.Extensions.AI patterns.
/// </summary>
public class AgentMemory(Guid id) : DomainEntity<Guid>(id)
{
    public Guid SessionId { get; private set; }
    public MemoryRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Vector embedding of the Content for semantic similarity search.
    /// Converted to/from pgvector.Vector at the EF Core data mapping boundary.
    /// </summary>
    public ReadOnlyMemory<float>? Embedding { get; private set; }

    public string? Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public AgentMemory(Guid id, Guid sessionId, MemoryRole role, string content, ReadOnlyMemory<float>? embedding = null, string? metadata = null) : this(id)
    {
        SessionId = sessionId;
        Role = role;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Embedding = embedding;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
    }
}
