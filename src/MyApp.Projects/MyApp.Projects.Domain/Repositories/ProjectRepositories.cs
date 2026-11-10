using MyApp.Projects.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Projects.Domain.Repositories;

/// <summary>Repository for <see cref="Project"/>.</summary>
public interface IProjectRepository : IRepository<Project, Guid>
{
    /// <summary>Gets a project by number.</summary>
    Task<Project?> GetByNumberAsync(string projectNumber, CancellationToken cancellationToken = default);

    /// <summary>Counts projects whose number starts with <paramref name="prefix"/>.</summary>
    Task<int> CountByNumberPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>Lists all projects (read-only).</summary>
    Task<List<Project>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="ProjectTask"/>.</summary>
public interface IProjectTaskRepository : IRepository<ProjectTask, Guid>
{
    /// <summary>Gets all tasks of a project (tracked).</summary>
    Task<List<ProjectTask>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="ProjectPlanningLine"/>.</summary>
public interface IProjectPlanningLineRepository : IRepository<ProjectPlanningLine, Guid>
{
    /// <summary>Gets planning lines of a project, optionally filtered by task (tracked).</summary>
    Task<List<ProjectPlanningLine>> GetByProjectAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default);

    /// <summary>Gets planning lines by ids (tracked).</summary>
    Task<List<ProjectPlanningLine>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Returns whether any planning line exists for the task.</summary>
    Task<bool> AnyForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Gets the highest line number used in the project.</summary>
    Task<int> GetMaxLineNoAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="ProjectLedgerEntry"/> (append-only).</summary>
public interface IProjectLedgerEntryRepository
{
    /// <summary>Adds an entry.</summary>
    Task AddAsync(ProjectLedgerEntry entry);

    /// <summary>Gets entries of a project (read-only).</summary>
    Task<List<ProjectLedgerEntry>> GetByProjectAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns whether any entry exists for the project.</summary>
    Task<bool> AnyForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Returns whether any entry exists for the task.</summary>
    Task<bool> AnyForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>Returns whether any entry exists for the planning line.</summary>
    Task<bool> AnyForPlanningLineAsync(Guid planningLineId, CancellationToken cancellationToken = default);

    /// <summary>Gets the highest entry number used in the project.</summary>
    Task<int> GetMaxEntryNoAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="ProjectInvoiceRequest"/>.</summary>
public interface IProjectInvoiceRequestRepository : IRepository<ProjectInvoiceRequest, Guid>
{
    /// <summary>Gets a request with its lines (tracked).</summary>
    Task<ProjectInvoiceRequest?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets requests of a project with their lines (read-only).</summary>
    Task<List<ProjectInvoiceRequest>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="ProjectConsumptionOrder"/>.</summary>
public interface IProjectConsumptionOrderRepository : IRepository<ProjectConsumptionOrder, Guid>
{
    /// <summary>Gets a consumption order by the Orders-service order id, with lines (tracked).</summary>
    Task<ProjectConsumptionOrder?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Gets consumption orders of a project with lines (read-only).</summary>
    Task<List<ProjectConsumptionOrder>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="Resource"/>.</summary>
public interface IResourceRepository : IRepository<Resource, Guid>
{
    /// <summary>Gets a resource by number.</summary>
    Task<Resource?> GetByNumberAsync(string resourceNo, CancellationToken cancellationToken = default);

    /// <summary>Lists all resources with their group (read-only).</summary>
    Task<List<Resource>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns whether the resource is used by planning lines or ledger entries.</summary>
    Task<bool> IsInUseAsync(Guid resourceId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="ResourceGroup"/>.</summary>
public interface IResourceGroupRepository : IRepository<ResourceGroup, Guid>
{
    /// <summary>Gets a resource group by code.</summary>
    Task<ResourceGroup?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Returns whether any resource uses the group.</summary>
    Task<bool> IsInUseAsync(Guid groupId, CancellationToken cancellationToken = default);
}

/// <summary>Repository for <see cref="Currency"/> and its exchange rates.</summary>
public interface ICurrencyRepository : IRepository<Currency, Guid>
{
    /// <summary>Gets a currency by ISO code with its exchange rates.</summary>
    Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Lists all currencies (read-only).</summary>
    Task<List<Currency>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the rate (LCY per unit) valid at <paramref name="date"/>, or null when none exists.</summary>
    Task<decimal?> GetRateAsync(string currencyCode, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>Adds an exchange rate.</summary>
    Task AddExchangeRateAsync(ExchangeRate rate);

    /// <summary>Gets an exchange rate by id (tracked).</summary>
    Task<ExchangeRate?> GetExchangeRateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Removes an exchange rate.</summary>
    void RemoveExchangeRate(ExchangeRate rate);

    /// <summary>Returns whether any project uses the currency.</summary>
    Task<bool> IsInUseAsync(string currencyCode, CancellationToken cancellationToken = default);
}
