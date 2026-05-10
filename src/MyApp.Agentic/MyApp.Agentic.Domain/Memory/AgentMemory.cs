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
/// Implements VectorStore record pattern for integration with Microsoft.Extensions.VectorData.
/// </summary>
[VectorStoreRecord]
public class AgentMemory(Guid id) : DomainEntity<Guid>(id)
{
    [VectorStoreRecordKey]
    public new Guid Id => base.Id;

    [VectorStoreRecordData]
    public Guid SessionId { get; private set; }
    
    [VectorStoreRecordData]
    public MemoryRole Role { get; private set; }
    
    [VectorStoreRecordData]
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Vector embedding of the Content for semantic similarity search.
    /// Converted to/from pgvector.Vector at the EF Core data mapping boundary.
    /// </summary>
    [VectorStoreRecordVector(1536, "CosineSimilarity")]
    public ReadOnlyMemory<float>? Embedding { get; private set; }

    [VectorStoreRecordData]
    public string? Metadata { get; private set; }
    
    [VectorStoreRecordData]
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
