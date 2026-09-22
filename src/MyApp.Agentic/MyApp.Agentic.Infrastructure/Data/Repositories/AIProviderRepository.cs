using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

/// <summary>EF Core repository for <see cref="AIProvider"/> entities, eagerly loading the associated models collection.</summary>
public class AIProviderRepository : Repository<AIProvider, Guid>, IAIProviderRepository
{
    private readonly AgenticSqlDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AIProviderRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public AIProviderRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets an item by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public override async Task<AIProvider?> GetByIdAsync(Guid id)
    {
        return await _context.AIProviders
            .Include(p => p.Models)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}