using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Repositories;
using System.Linq.Expressions;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AgentRepository : Repository<Agent, Guid>, IAgentRepository
{
    private readonly AgenticSqlDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AgentRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public AgentRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets an item by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public override async Task<Agent?> GetByIdAsync(Guid id)
    {
        return await _context.Agents
            .Include(a => a.Model)
                .ThenInclude(m => m!.Provider)
            .Include(a => a.Plugins)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>
    /// Updates an existing item asynchronously.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public override async Task<Agent> UpdateAsync(Agent entity)
    {
        var entry = _context.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            _context.Agents.Attach(entity);
            entry = _context.Entry(entity);
        }

        entry.State = EntityState.Modified;
        entry.Property(a => a.ModelId).IsModified = true;

        await _context.Agents
            .Where(a => a.Id == entity.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.ModelId, entity.ModelId));

        return entity;
    }

    /// <summary>
    /// Gets the id with details asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<Agent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Agents
            .Include(a => a.Model)
                .ThenInclude(m => m!.Provider)
            .Include(a => a.Plugins)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets all items asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public override  async Task<IEnumerable<Agent>> GetAllAsync()
    {
        return await DbContext.Set<Agent>().Include(x => x.Model).ToListAsync();
    }

    /// <summary>
    /// Gets a paginated list of items asynchronously.
    /// </summary>
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <param name="includes">The includes.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public override Task<PaginatedResult<Agent>> GetAllPaginatedAsync(int pageNumber, int pageSize, IEnumerable<Expression<Func<Agent, object>>>? includes = null)
    {
        return base.GetAllPaginatedAsync(pageNumber, pageSize, includes);
    }
}