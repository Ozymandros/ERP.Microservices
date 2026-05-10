using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Infrastructure.Data;
using Pgvector;

namespace MyApp.Agentic.Infrastructure.Memory;

public interface IMemoryRepository
{
    Task<IEnumerable<AgentMemory>> GetRecentMemoriesAsync(Guid sessionId, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<AgentMemory>> SearchSimilarAsync(Guid sessionId, ReadOnlyMemory<float> embedding, int topK = 3, CancellationToken cancellationToken = default);
    Task AddMemoryAsync(AgentMemory memory, CancellationToken cancellationToken = default);
    Task AddMemoriesAsync(IEnumerable<AgentMemory> memories, CancellationToken cancellationToken = default);
}

public class MemoryRepository : IMemoryRepository
{
    private readonly MemoryDbContext _context;

    public MemoryRepository(MemoryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AgentMemory>> GetRecentMemoriesAsync(Guid sessionId, int count, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgentMemory>> SearchSimilarAsync(Guid sessionId, ReadOnlyMemory<float> embedding, int topK = 3, CancellationToken cancellationToken = default)
    {
        // Load all memories for the session with embeddings
        var memories = await _context.AgentMemories
            .Where(m => m.SessionId == sessionId && m.Embedding.HasValue)
            .ToListAsync(cancellationToken);

        if (!memories.Any())
            return Enumerable.Empty<AgentMemory>();

        // Calculate cosine similarity in-memory
        var queryVector = embedding.ToArray();
        var results = memories
            .Select(m => new
            {
                Memory = m,
                Similarity = CosineSimilarity(queryVector, m.Embedding!.Value.ToArray())
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .Select(x => x.Memory)
            .ToList();

        return results;
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Vectors must have the same length");

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0f || magnitudeB == 0f)
            return 0f;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    public async Task AddMemoryAsync(AgentMemory memory, CancellationToken cancellationToken = default)
    {
        await _context.AgentMemories.AddAsync(memory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMemoriesAsync(IEnumerable<AgentMemory> memories, CancellationToken cancellationToken = default)
    {
        await _context.AgentMemories.AddRangeAsync(memories, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
