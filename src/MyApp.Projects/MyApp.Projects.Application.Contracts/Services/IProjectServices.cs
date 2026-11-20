using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Domain.Entities;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Projects.Application.Contracts.Services;

/// <summary>Project header operations.</summary>
public interface IProjectService
{
    /// <summary>Lists all projects.</summary>
    Task<List<ProjectDto>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Queries projects.</summary>
    Task<PaginatedResult<ProjectDto>> QueryAsync(ISpecification<Project> spec, CancellationToken cancellationToken = default);
    /// <summary>Gets a project.</summary>
    Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Gets a project by number.</summary>
    Task<ProjectDto?> GetByNumberAsync(string projectNumber, CancellationToken cancellationToken = default);
    /// <summary>Creates a project.</summary>
    Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
    /// <summary>Updates a project header.</summary>
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a project (only Planning/Quote and without ledger entries).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Changes the project status.</summary>
    Task<ProjectDto> ChangeStatusAsync(Guid id, ProjectStatus status, CancellationToken cancellationToken = default);
    /// <summary>Copies a project (tasks + optional planning lines).</summary>
    Task<ProjectDto> CopyAsync(Guid id, CopyProjectDto dto, CancellationToken cancellationToken = default);
    /// <summary>Gets project statistics (rolled up through the task tree).</summary>
    Task<ProjectStatisticsDto> GetStatisticsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Task tree operations.</summary>
public interface IProjectTaskService
{
    /// <summary>Lists the tasks of a project (flat, ordered).</summary>
    Task<List<ProjectTaskDto>> ListAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>Gets the task tree of a project.</summary>
    Task<List<ProjectTaskTreeNodeDto>> GetTreeAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>Gets a task.</summary>
    Task<ProjectTaskDto?> GetByIdAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default);
    /// <summary>Creates a task.</summary>
    Task<ProjectTaskDto> CreateAsync(Guid projectId, CreateProjectTaskDto dto, CancellationToken cancellationToken = default);
    /// <summary>Updates a task.</summary>
    Task<ProjectTaskDto> UpdateAsync(Guid projectId, Guid taskId, UpdateProjectTaskDto dto, CancellationToken cancellationToken = default);
    /// <summary>Moves (re-parents / re-orders) a task.</summary>
    Task<ProjectTaskDto> MoveAsync(Guid projectId, Guid taskId, MoveProjectTaskDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a task (no children, planning lines or ledger entries).</summary>
    Task DeleteAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken = default);
}

/// <summary>Planning line operations.</summary>
public interface IPlanningLineService
{
    /// <summary>Lists planning lines, optionally for one task.</summary>
    Task<List<PlanningLineDto>> ListAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default);
    /// <summary>Gets a planning line.</summary>
    Task<PlanningLineDto?> GetByIdAsync(Guid projectId, Guid lineId, CancellationToken cancellationToken = default);
    /// <summary>Creates a planning line.</summary>
    Task<PlanningLineDto> CreateAsync(Guid projectId, CreateUpdatePlanningLineDto dto, CancellationToken cancellationToken = default);
    /// <summary>Updates a planning line.</summary>
    Task<PlanningLineDto> UpdateAsync(Guid projectId, Guid lineId, CreateUpdatePlanningLineDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a planning line (nothing posted, consumed or invoiced).</summary>
    Task DeleteAsync(Guid projectId, Guid lineId, CancellationToken cancellationToken = default);
}

/// <summary>Usage, consumption and invoicing (posting) operations, including integration event handling.</summary>
public interface IProjectPostingService
{
    /// <summary>Lists ledger entries.</summary>
    Task<List<ProjectLedgerEntryDto>> GetLedgerEntriesAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default);
    /// <summary>Posts resource/expense usage.</summary>
    Task<ProjectLedgerEntryDto> PostUsageAsync(Guid projectId, PostUsageDto dto, CancellationToken cancellationToken = default);
    /// <summary>Creates an Outbound consumption order in Orders for item planning lines.</summary>
    Task<ConsumptionOrderDto> CreateItemConsumptionAsync(Guid projectId, CreateItemConsumptionDto dto, CancellationToken cancellationToken = default);
    /// <summary>Lists consumption orders.</summary>
    Task<List<ConsumptionOrderDto>> GetConsumptionOrdersAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>Creates an invoice request and publishes it to Billing.</summary>
    Task<InvoiceRequestDto> RequestInvoiceAsync(Guid projectId, CreateInvoiceRequestDto dto, CancellationToken cancellationToken = default);
    /// <summary>Lists invoice requests.</summary>
    Task<List<InvoiceRequestDto>> GetInvoiceRequestsAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Handles <c>orders.order.fulfilled</c> for project consumption orders. Returns the project id when handled.</summary>
    Task<Guid?> HandleOrderFulfilledAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken = default);
    /// <summary>Handles <c>orders.order.cancelled</c> for project consumption orders. Returns the project id when handled.</summary>
    Task<Guid?> HandleOrderCancelledAsync(OrderCancelledEvent @event, CancellationToken cancellationToken = default);
    /// <summary>Handles <c>billing.invoice.created</c> for project invoices. Returns the project id when handled.</summary>
    Task<Guid?> HandleInvoiceCreatedAsync(InvoiceCreatedEvent @event, CancellationToken cancellationToken = default);
    /// <summary>Handles <c>billing.invoice.issued</c> for project invoices. Returns the project id when handled.</summary>
    Task<Guid?> HandleInvoiceIssuedAsync(InvoiceIssuedEvent @event, CancellationToken cancellationToken = default);
    /// <summary>Handles <c>billing.invoice.cancelled</c> for project invoices. Returns the project id when handled.</summary>
    Task<Guid?> HandleInvoiceCancelledAsync(InvoiceCancelledEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>Resource and resource group operations.</summary>
public interface IResourceService
{
    /// <summary>Lists resources.</summary>
    Task<List<ResourceDto>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Queries resources.</summary>
    Task<PaginatedResult<ResourceDto>> QueryAsync(ISpecification<Resource> spec, CancellationToken cancellationToken = default);
    /// <summary>Gets a resource.</summary>
    Task<ResourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates a resource.</summary>
    Task<ResourceDto> CreateAsync(CreateUpdateResourceDto dto, CancellationToken cancellationToken = default);
    /// <summary>Updates a resource.</summary>
    Task<ResourceDto> UpdateAsync(Guid id, CreateUpdateResourceDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a resource.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists resource groups.</summary>
    Task<List<ResourceGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets a resource group.</summary>
    Task<ResourceGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Creates a resource group.</summary>
    Task<ResourceGroupDto> CreateGroupAsync(CreateUpdateResourceGroupDto dto, CancellationToken cancellationToken = default);
    /// <summary>Updates a resource group.</summary>
    Task<ResourceGroupDto> UpdateGroupAsync(Guid id, CreateUpdateResourceGroupDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a resource group (not in use).</summary>
    Task DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Currency and exchange rate operations.</summary>
public interface ICurrencyService
{
    /// <summary>Gets the local currency code.</summary>
    string LocalCurrency { get; }
    /// <summary>Lists currencies.</summary>
    Task<List<CurrencyDto>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets a currency by code.</summary>
    Task<CurrencyDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    /// <summary>Creates a currency.</summary>
    Task<CurrencyDto> CreateAsync(CreateUpdateCurrencyDto dto, CancellationToken cancellationToken = default);
    /// <summary>Updates a currency's description/symbol.</summary>
    Task<CurrencyDto> UpdateAsync(string code, CreateUpdateCurrencyDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes a currency (not used by any project).</summary>
    Task DeleteAsync(string code, CancellationToken cancellationToken = default);
    /// <summary>Adds an exchange rate.</summary>
    Task<ExchangeRateDto> AddExchangeRateAsync(string code, CreateExchangeRateDto dto, CancellationToken cancellationToken = default);
    /// <summary>Deletes an exchange rate.</summary>
    Task DeleteExchangeRateAsync(string code, Guid rateId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the exchange rate (LCY per 1 unit) of <paramref name="currencyCode"/> at <paramref name="date"/>;
    /// 1 for the local currency. Throws when no rate exists.
    /// </summary>
    Task<decimal> GetRateAsync(string currencyCode, DateTime date, CancellationToken cancellationToken = default);
}
