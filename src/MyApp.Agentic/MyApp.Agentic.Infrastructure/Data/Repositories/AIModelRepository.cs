using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

/// <summary>EF Core repository for <see cref="AIModel"/> entities, eagerly loading the parent <see cref="Domain.AIProviders.AIProvider"/>.</summary>
public class AIModelRepository : Repository<AIModel, Guid>, IAIModelRepository
{
    private readonly AgenticSqlDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AIModelRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public AIModelRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets an item by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public override async Task<AIModel?> GetByIdAsync(Guid id)
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <summary>
    /// Gets all items asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public override async Task<IEnumerable<AIModel>> GetAllAsync()
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .OrderBy(m => m.Provider!.Name)
            .ThenBy(m => m.CommercialName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the provider id asynchronously.
    /// </summary>
    /// <param name="providerId">The provider Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<AIModel>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _context.AIModels
            .Include(m => m.Provider)
            .Where(m => m.ProviderId == providerId)
            .OrderBy(m => m.CommercialName)
            .ToListAsync(cancellationToken);
    }
}