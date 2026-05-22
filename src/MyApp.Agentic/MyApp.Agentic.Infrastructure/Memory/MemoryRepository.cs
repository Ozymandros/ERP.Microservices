using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Repositories;
using System.Data;
using System.Globalization;

namespace MyApp.Agentic.Infrastructure.Memory;

/// <summary>
/// Persists and retrieves agent conversation memories from SQL Server.
/// </summary>
public interface IMemoryRepository
{
    /// <summary>
    /// Gets the ordered session transcript without vector similarity ranking.
    /// </summary>
    /// <param name="sessionId">Conversation session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Chronological session messages.</returns>
    Task<IReadOnlyList<AgentMemory>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent memories for a session.
    /// </summary>
    /// <param name="sessionId">Conversation session identifier.</param>
    /// <param name="count">Maximum number of memories to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recent session memories.</returns>
    Task<IEnumerable<AgentMemory>> GetRecentMemoriesAsync(Guid sessionId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs vector similarity search over memories in a session.
    /// </summary>
    /// <param name="sessionId">Conversation session identifier.</param>
    /// <param name="query">User query text to embed and search against.</param>
    /// <param name="embeddingProvider">Provider context used to generate the query embedding.</param>
    /// <param name="topK">Maximum number of similar memories to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Most similar memories for retrieval-augmented generation.</returns>
    Task<IEnumerable<AgentMemory>> SearchSimilarAsync(
        Guid sessionId,
        string query,
        MemoryEmbeddingProviderContext embeddingProvider,
        int topK = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a single memory row for a session.
    /// </summary>
    /// <param name="memory">Memory entity to store.</param>
    /// <param name="embeddingProvider">Provider context used when embeddings are generated.</param>
    /// <param name="generateEmbedding">Whether to generate and store an embedding vector.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddMemoryAsync(
        AgentMemory memory,
        MemoryEmbeddingProviderContext embeddingProvider,
        bool generateEmbedding = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists multiple memory rows for a session.
    /// </summary>
    /// <param name="memories">Memory entities to store.</param>
    /// <param name="embeddingProvider">Provider context used when embeddings are generated.</param>
    /// <param name="generateEmbeddings">Whether to generate and store embedding vectors.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddMemoriesAsync(
        IEnumerable<AgentMemory> memories,
        MemoryEmbeddingProviderContext embeddingProvider,
        bool generateEmbeddings = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// SQL Server implementation of <see cref="IMemoryRepository"/>.
/// </summary>
public class MemoryRepository : DbContextRepositoryBase, IMemoryRepository
{
    private readonly AgenticSqlDbContext _context;
    private readonly IMemoryEmbeddingGenerator _embeddingGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRepository"/> class.
    /// </summary>
    /// <param name="context">Agentic SQL database context.</param>
    /// <param name="embeddingGenerator">Embedding generator used for vector search and memory indexing.</param>
    public MemoryRepository(AgenticSqlDbContext context, IMemoryEmbeddingGenerator embeddingGenerator)
        : base(context)
    {
        _context = context;
        _embeddingGenerator = embeddingGenerator;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentMemory>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentMemories
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMemory>> GetRecentMemoriesAsync(Guid sessionId, int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            return Enumerable.Empty<AgentMemory>();

        return await _context.AgentMemories
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMemory>> SearchSimilarAsync(
        Guid sessionId,
        string query,
        MemoryEmbeddingProviderContext embeddingProvider,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<AgentMemory>();

        if (topK <= 0)
            return Enumerable.Empty<AgentMemory>();

        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(query.Trim(), embeddingProvider, cancellationToken);
        var embeddingLiteral = SerializeVector(embedding);

        const string sql = """
            SELECT TOP (@topK)
                [Id],
                [SessionId],
                [Role],
                [Content],
                [Metadata],
                [CreatedAt]
            FROM [AgentMemories]
            WHERE [SessionId] = @sessionId
              AND [EmbeddingVector] IS NOT NULL
            ORDER BY VECTOR_DISTANCE('cosine', [EmbeddingVector], CAST(@queryVector AS vector(1536)))
            """;

        // Never dispose DbContext.Database.GetDbConnection(): it is owned by the context.
        // `await using` on that connection disposes it and breaks later SaveChanges on the same scope.
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var topKParam = new SqlParameter("@topK", SqlDbType.Int) { Value = topK };
            var sessionParam = new SqlParameter("@sessionId", SqlDbType.UniqueIdentifier) { Value = sessionId };
            var vectorParam = new SqlParameter("@queryVector", SqlDbType.NVarChar) { Value = embeddingLiteral };

            command.Parameters.Add(topKParam);
            command.Parameters.Add(sessionParam);
            command.Parameters.Add(vectorParam);

            var results = new List<AgentMemory>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var recordSessionId = reader.GetGuid(1);
                var role = ParseRole(reader.GetString(2));
                var content = reader.GetString(3);
                var metadata = reader.IsDBNull(4) ? null : reader.GetString(4);
                var createdAt = reader.GetDateTime(5);

                var memory = new AgentMemory(id, recordSessionId, role, content, metadata)
                {
                    CreatedAt = createdAt
                };

                results.Add(memory);
            }

            return results;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    /// <inheritdoc />
    public Task AddMemoryAsync(
        AgentMemory memory,
        MemoryEmbeddingProviderContext embeddingProvider,
        bool generateEmbedding = true,
        CancellationToken cancellationToken = default)
    {
        return AddMemoriesAsync([memory], embeddingProvider, generateEmbedding, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddMemoriesAsync(
        IEnumerable<AgentMemory> memories,
        MemoryEmbeddingProviderContext embeddingProvider,
        bool generateEmbeddings = true,
        CancellationToken cancellationToken = default)
    {
        var memoryList = memories.ToList();
        if (memoryList.Count == 0)
            return;

        if (!generateEmbeddings)
        {
            _context.AgentMemories.AddRange(memoryList);
            await base.SaveChangesAsync(disableTracking: true, cancellationToken);
            return;
        }

        var prepared = new List<(AgentMemory Memory, float[] Embedding)>(memoryList.Count);
        foreach (var memory in memoryList)
        {
            var embedding = memory.Embedding ?? await _embeddingGenerator.GenerateEmbeddingAsync(memory.Content ?? string.Empty, embeddingProvider, cancellationToken);
            prepared.Add((memory, embedding));
        }

        foreach (var (memory, embedding) in prepared)
        {
            _context.AgentMemories.Add(memory);
            _context.Entry(memory).Property("EmbeddingVector").CurrentValue = new SqlVector<float>(embedding);
        }

        await base.SaveChangesAsync(disableTracking: true,cancellationToken);
    }

    private static MemoryRole ParseRole(string value)
    {
        return Enum.TryParse<MemoryRole>(value, ignoreCase: true, out var role)
            ? role
            : MemoryRole.User;
    }

    private static string SerializeVector(float[] values)
    {
        var serialized = string.Join(',', values.Select(v => v.ToString("G9", CultureInfo.InvariantCulture)));
        return $"[{serialized}]";
    }
}
