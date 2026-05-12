using MyApp.Shared.Domain.Entities;
using Microsoft.Extensions.VectorData;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Agentic.Domain.Memory;

public enum MemoryRole
{
    User,
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

    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData]
    public Guid SessionId { get; set; }

    [VectorStoreData]
    public MemoryRole Role { get; set; }

    [VectorStoreData]
    public string? Content { get; set; }

    /// <summary>
    /// Optional in-memory embedding payload; excluded from EF persistence.
    /// </summary>
    [VectorStoreVector(1536, DistanceFunction = Microsoft.Extensions.VectorData.DistanceFunction.CosineSimilarity)]
    [NotMapped]
    public float[]? Embedding => _embedding;

    [VectorStoreData]
    public string? Metadata { get; set; }

    [VectorStoreData]
    public DateTime CreatedAt { get; set; }
}
