using Microsoft.EntityFrameworkCore;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Projects.Infrastructure.Data.Repositories;

/// <summary>EF implementation of <see cref="IProjectRepository"/>.</summary>
public class ProjectRepository(ProjectsDbContext context) : Repository<Project, Guid>(context), IProjectRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<Project?> GetByNumberAsync(string projectNumber, CancellationToken cancellationToken = default) =>
        _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectNumber == projectNumber, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountByNumberPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        _db.Projects.CountAsync(p => p.ProjectNumber.StartsWith(prefix), cancellationToken);

    /// <inheritdoc />
    public Task<List<Project>> ListAsync(CancellationToken cancellationToken = default) =>
        _db.Projects.AsNoTracking().OrderBy(p => p.ProjectNumber).ToListAsync(cancellationToken);
}

/// <summary>EF implementation of <see cref="IProjectTaskRepository"/>.</summary>
public class ProjectTaskRepository(ProjectsDbContext context) : Repository<ProjectTask, Guid>(context), IProjectTaskRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<List<ProjectTask>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _db.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.TaskNo)
            .ToListAsync(cancellationToken);
}

/// <summary>EF implementation of <see cref="IProjectPlanningLineRepository"/>.</summary>
public class ProjectPlanningLineRepository(ProjectsDbContext context)
    : Repository<ProjectPlanningLine, Guid>(context), IProjectPlanningLineRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<List<ProjectPlanningLine>> GetByProjectAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.PlanningLines.Where(l => l.ProjectId == projectId);
        if (taskId.HasValue)
            query = query.Where(l => l.ProjectTaskId == taskId.Value);
        return query.OrderBy(l => l.LineNo).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<ProjectPlanningLine>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return _db.PlanningLines.Where(l => idList.Contains(l.Id)).OrderBy(l => l.LineNo).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        _db.PlanningLines.AnyAsync(l => l.ProjectTaskId == taskId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetMaxLineNoAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _db.PlanningLines.Where(l => l.ProjectId == projectId).MaxAsync(l => (int?)l.LineNo, cancellationToken) ?? 0;
}

/// <summary>EF implementation of <see cref="IProjectLedgerEntryRepository"/>.</summary>
public class ProjectLedgerEntryRepository(ProjectsDbContext context) : IProjectLedgerEntryRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public async Task AddAsync(ProjectLedgerEntry entry) => await _db.LedgerEntries.AddAsync(entry);

    /// <inheritdoc />
    public Task<List<ProjectLedgerEntry>> GetByProjectAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.LedgerEntries.AsNoTracking().Where(e => e.ProjectId == projectId);
        if (taskId.HasValue)
            query = query.Where(e => e.ProjectTaskId == taskId.Value);
        return query.OrderBy(e => e.EntryNo).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyForProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _db.LedgerEntries.AnyAsync(e => e.ProjectId == projectId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AnyForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        _db.LedgerEntries.AnyAsync(e => e.ProjectTaskId == taskId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> AnyForPlanningLineAsync(Guid planningLineId, CancellationToken cancellationToken = default) =>
        _db.LedgerEntries.AnyAsync(e => e.PlanningLineId == planningLineId, cancellationToken);

    /// <inheritdoc />
    public async Task<int> GetMaxEntryNoAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _db.LedgerEntries.Where(e => e.ProjectId == projectId).MaxAsync(e => (int?)e.EntryNo, cancellationToken) ?? 0;
}

/// <summary>EF implementation of <see cref="IProjectInvoiceRequestRepository"/>.</summary>
public class ProjectInvoiceRequestRepository(ProjectsDbContext context)
    : Repository<ProjectInvoiceRequest, Guid>(context), IProjectInvoiceRequestRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<ProjectInvoiceRequest?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.InvoiceRequests.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<List<ProjectInvoiceRequest>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _db.InvoiceRequests.AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);
}

/// <summary>EF implementation of <see cref="IProjectConsumptionOrderRepository"/>.</summary>
public class ProjectConsumptionOrderRepository(ProjectsDbContext context)
    : Repository<ProjectConsumptionOrder, Guid>(context), IProjectConsumptionOrderRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<ProjectConsumptionOrder?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _db.ConsumptionOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

    /// <inheritdoc />
    public Task<List<ProjectConsumptionOrder>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        _db.ConsumptionOrders.AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.ProjectId == projectId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
}

/// <summary>EF implementation of <see cref="IResourceRepository"/>.</summary>
public class ResourceRepository(ProjectsDbContext context) : Repository<Resource, Guid>(context), IResourceRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<Resource?> GetByNumberAsync(string resourceNo, CancellationToken cancellationToken = default) =>
        _db.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.ResourceNo == resourceNo, cancellationToken);

    /// <inheritdoc />
    public Task<List<Resource>> ListAsync(CancellationToken cancellationToken = default) =>
        _db.Resources.AsNoTracking().Include(r => r.ResourceGroup).OrderBy(r => r.ResourceNo).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsInUseAsync(Guid resourceId, CancellationToken cancellationToken = default) =>
        await _db.PlanningLines.AnyAsync(l => l.ResourceId == resourceId, cancellationToken)
        || await _db.LedgerEntries.AnyAsync(e => e.ResourceId == resourceId, cancellationToken);
}

/// <summary>EF implementation of <see cref="IResourceGroupRepository"/>.</summary>
public class ResourceGroupRepository(ProjectsDbContext context) : Repository<ResourceGroup, Guid>(context), IResourceGroupRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<ResourceGroup?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.ResourceGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Code == code, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsInUseAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        _db.Resources.AnyAsync(r => r.ResourceGroupId == groupId, cancellationToken);
}

/// <summary>EF implementation of <see cref="ICurrencyRepository"/>.</summary>
public class CurrencyRepository(ProjectsDbContext context) : Repository<Currency, Guid>(context), ICurrencyRepository
{
    private readonly ProjectsDbContext _db = context;

    /// <inheritdoc />
    public Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.Currencies
            .Include(c => c.ExchangeRates)
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper(), cancellationToken);

    /// <inheritdoc />
    public Task<List<Currency>> ListAsync(CancellationToken cancellationToken = default) =>
        _db.Currencies.AsNoTracking().Include(c => c.ExchangeRates).OrderBy(c => c.Code).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<decimal?> GetRateAsync(string currencyCode, DateTime date, CancellationToken cancellationToken = default)
    {
        var code = currencyCode.ToUpper();
        var day = date.Date;
        return await _db.ExchangeRates.AsNoTracking()
            .Where(r => r.Currency!.Code == code && r.StartingDate <= day)
            .OrderByDescending(r => r.StartingDate)
            .Select(r => (decimal?)r.RateToLocal)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddExchangeRateAsync(ExchangeRate rate) => await _db.ExchangeRates.AddAsync(rate);

    /// <inheritdoc />
    public Task<ExchangeRate?> GetExchangeRateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ExchangeRates.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public void RemoveExchangeRate(ExchangeRate rate) => _db.ExchangeRates.Remove(rate);

    /// <inheritdoc />
    public Task<bool> IsInUseAsync(string currencyCode, CancellationToken cancellationToken = default) =>
        _db.Projects.AnyAsync(p => p.CurrencyCode == currencyCode.ToUpper(), cancellationToken);
}
