using Microsoft.Extensions.VectorData;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Agentic.Domain.Memory;

/// <summary>
/// Identifies which party authored a conversation memory entry.
/// </summary>
public enum MemoryRole
{
    /// <summary>The entry was authored by the human user.</summary>
    User,
    /// <summary>The entry was authored by the AI assistant.</summary>
    Assistant
}

/// <summary>
/// Represents a conversation memory entry with vector-store metadata for similarity search.
/// </summary>
public class AgentMemory
{
    private readonly float[]? _embedding;

    // Parameterless constructor for EF Core
    private AgentMemory() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Initializes a new instance of the AgentMemory class.
    /// </summary>
    /// <param name="sessionId">The session Id.</param>
    /// <param name="role">The role.</param>
    /// <param name="content">The content.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="embedding">The embedding.</param>
    public AgentMemory(Guid sessionId, MemoryRole role, string content, string? metadata = null, float[]? embedding = null)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        Role = role;
        Content = content;
        Metadata = metadata;
        _embedding = embedding;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the AgentMemory class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="sessionId">The session Id.</param>
    /// <param name="role">The role.</param>
    /// <param name="content">The content.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="embedding">The embedding.</param>
    public AgentMemory(Guid id, Guid sessionId, MemoryRole role, string content, string? metadata = null, float[]? embedding = null)
    {
        Id = id;
        SessionId = sessionId;
        Role = role;
        Content = content;
        Metadata = metadata;
        _embedding = embedding;
        CreatedAt = DateTime.UtcNow;
    }
    // ... propiedades ...

    /// <summary>Gets or sets the unique identifier for this memory entry.</summary>
    [VectorStoreKey]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the conversation session this entry belongs to.</summary>
    [VectorStoreData]
    public Guid SessionId { get; set; }

    /// <summary>Gets or sets the role of the message author.</summary>
    [VectorStoreData]
    public MemoryRole Role { get; set; }

    /// <summary>Gets or sets the text content of the conversation turn.</summary>
    [VectorStoreData]
    public string? Content { get; set; }

    /// <summary>
    /// Optional in-memory embedding payload; excluded from EF persistence.
    /// </summary>
    [VectorStoreVector(1536, DistanceFunction = Microsoft.Extensions.VectorData.DistanceFunction.CosineSimilarity)]
    [NotMapped]
    public float[]? Embedding => _embedding;

    /// <summary>Gets or sets optional JSON metadata associated with this entry.</summary>
    [VectorStoreData]
    public string? Metadata { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this entry was created.</summary>
    [VectorStoreData]
    public DateTime CreatedAt { get; set; }
}
