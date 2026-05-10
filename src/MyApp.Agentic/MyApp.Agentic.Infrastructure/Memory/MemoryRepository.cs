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
        // Convert ReadOnlyMemory<float> to Pgvector.Vector for typed parameter passing
        var vector = new Vector(embedding.ToArray());

        var sql = @"
            SELECT * FROM ""AgentMemories""
            WHERE ""SessionId"" = {0} AND ""Embedding"" IS NOT NULL
            ORDER BY ""Embedding"" <=> {1}
            LIMIT {2}";

        var results = await _context.AgentMemories
            .FromSqlRaw(sql, sessionId, vector, topK)
            .ToListAsync(cancellationToken);

        return results;
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
