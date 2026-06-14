using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Repositories;
using System.Linq.Expressions;

namespace MyApp.Agentic.Infrastructure.Data.Repositories;

public class AgentRepository : Repository<Agent, Guid>, IAgentRepository
{
    private readonly AgenticSqlDbContext _context;

    public AgentRepository(AgenticSqlDbContext context) : base(context)
    {
        _context = context;
    }

    public override async Task<Agent?> GetByIdAsync(Guid id)
    {
        return await _context.Agents
            .Include(a => a.Model)
                .ThenInclude(m => m!.Provider)
            .Include(a => a.Plugins)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

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

    public async Task<Agent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Agents
            .Include(a => a.Model)
                .ThenInclude(m => m!.Provider)
            .Include(a => a.Plugins)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public override  async Task<IEnumerable<Agent>> GetAllAsync()
    {
        return await DbContext.Set<Agent>().Include(x => x.Model).ToListAsync();
    }

    public override Task<PaginatedResult<Agent>> GetAllPaginatedAsync(int pageNumber, int pageSize, IEnumerable<Expression<Func<Agent, object>>>? includes = null)
    {
        return base.GetAllPaginatedAsync(pageNumber, pageSize, includes);
    }
}