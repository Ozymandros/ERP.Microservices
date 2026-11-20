using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Integration;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Projects.Domain.Services;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Projects.Application.Services;

/// <summary>
/// Planning line operations. Unit cost defaults are in LCY; unit price defaults (resource/product prices in LCY)
/// are converted to the project currency with the exchange rate valid on the planning date.
/// </summary>
public class PlanningLineService : AppServiceBase, IPlanningLineService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectTaskRepository _tasks;
    private readonly IProjectPlanningLineRepository _lines;
    private readonly IProjectLedgerEntryRepository _ledger;
    private readonly IResourceRepository _resources;
    private readonly IInventoryProductClient _products;
    private readonly ICurrencyService _currencyService;
    private readonly IMapper _mapper;

    /// <summary>Initializes a new instance of the <see cref="PlanningLineService"/> class.</summary>
    public PlanningLineService(
        IProjectRepository projects,
        IProjectTaskRepository tasks,
        IProjectPlanningLineRepository lines,
        IProjectLedgerEntryRepository ledger,
        IResourceRepository resources,
        IInventoryProductClient products,
        ICurrencyService currencyService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<PlanningLineService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Projects)
    {
        _projects = projects;
        _tasks = tasks;
        _lines = lines;
        _ledger = ledger;
        _resources = resources;
        _products = products;
        _currencyService = currencyService;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<List<PlanningLineDto>> ListAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        await GetProjectAsync(projectId);
        return _mapper.Map<List<PlanningLineDto>>(await _lines.GetByProjectAsync(projectId, taskId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<PlanningLineDto?> GetByIdAsync(Guid projectId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await _lines.GetByIdAsync(lineId);
        return line is null || line.ProjectId != projectId ? null : _mapper.Map<PlanningLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<PlanningLineDto> CreateAsync(Guid projectId, CreateUpdatePlanningLineDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();
        await EnsureLeafPostingTaskAsync(projectId, dto.ProjectTaskId, cancellationToken);

        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = projectId,
            LineNo = await _lines.GetMaxLineNoAsync(projectId, cancellationToken) + 10000,
            CurrencyCode = project.CurrencyCode
        };

        await ApplyAsync(line, dto, project, refreshRate: true, cancellationToken);
        await _lines.AddAsync(line);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<PlanningLineDto>(line);
    }

    /// <inheritdoc />
    public async Task<PlanningLineDto> UpdateAsync(Guid projectId, Guid lineId, CreateUpdatePlanningLineDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();

        var line = await _lines.GetByIdAsync(lineId);
        if (line is null || line.ProjectId != projectId)
            throw new NotFoundException(nameof(ProjectPlanningLine), lineId);

        var hasActivity = line.QuantityPosted != 0 || line.QuantityInConsumption != 0 || line.QuantityTransferredToInvoice != 0;
        if (hasActivity && (dto.Kind != line.Kind
                            || dto.ResourceId != line.ResourceId
                            || dto.ProductId != line.ProductId
                            || dto.ProjectTaskId != line.ProjectTaskId))
        {
            throw new InvalidOperationException(
                "Task, kind, resource and product cannot be changed once the line has usage, consumption or invoicing.");
        }

        if (dto.ProjectTaskId != line.ProjectTaskId)
            await EnsureLeafPostingTaskAsync(projectId, dto.ProjectTaskId, cancellationToken);

        // Keep the original rate once something was sent to invoicing (amounts already communicated to Billing).
        await ApplyAsync(line, dto, project, refreshRate: line.QuantityTransferredToInvoice == 0, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<PlanningLineDto>(line);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid projectId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanModify();

        var line = await _lines.GetByIdAsync(lineId);
        if (line is null || line.ProjectId != projectId)
            throw new NotFoundException(nameof(ProjectPlanningLine), lineId);

        if (line.QuantityPosted != 0 || line.QuantityInConsumption != 0 || line.QuantityTransferredToInvoice != 0
            || await _ledger.AnyForPlanningLineAsync(line.Id, cancellationToken))
            throw new InvalidOperationException("Planning lines with usage, consumption or invoicing cannot be deleted.");

        await _lines.DeleteAsync(line);
        await SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAsync(
        ProjectPlanningLine line,
        CreateUpdatePlanningLineDto dto,
        Project project,
        bool refreshRate,
        CancellationToken cancellationToken)
    {
        var planningDate = (dto.PlanningDate ?? DateTime.UtcNow).Date;
        if (refreshRate || line.ExchangeRate <= 0)
            line.ExchangeRate = await _currencyService.GetRateAsync(project.CurrencyCode, planningDate, cancellationToken);

        line.ProjectTaskId = dto.ProjectTaskId;
        line.LineType = dto.LineType;
        line.Kind = dto.Kind;
        line.PlanningDate = planningDate;
        line.Quantity = dto.Quantity;
        line.LineDiscountPercent = dto.LineDiscountPercent;
        line.UnitOfMeasure = dto.UnitOfMeasure;
        line.ResourceId = null;
        line.ProductId = null;
        line.WarehouseId = null;

        string? defaultDescription = null;
        decimal defaultCostLcy = 0;
        decimal defaultPriceLcy = 0;

        switch (dto.Kind)
        {
            case ProjectLineKind.Resource:
            {
                if (dto.ResourceId is null)
                    throw new ArgumentException("ResourceId is required for resource lines.");
                var resource = await _resources.GetByIdAsync(dto.ResourceId.Value)
                    ?? throw new ArgumentException($"Resource {dto.ResourceId} does not exist.");
                if (resource.Blocked)
                    throw new InvalidOperationException($"Resource {resource.ResourceNo} is blocked.");

                line.ResourceId = resource.Id;
                line.UnitOfMeasure ??= resource.BaseUnitOfMeasure;
                defaultDescription = resource.Name;
                defaultCostLcy = resource.UnitCost;
                defaultPriceLcy = resource.UnitPrice;
                break;
            }
            case ProjectLineKind.Item:
            {
                if (dto.ProductId is null)
                    throw new ArgumentException("ProductId is required for item lines.");
                var product = await _products.GetProductAsync(dto.ProductId.Value, cancellationToken)
                    ?? throw new ArgumentException($"Product {dto.ProductId} does not exist in Inventory.");

                line.ProductId = product.Id;
                line.WarehouseId = dto.WarehouseId;
                line.UnitOfMeasure ??= "PCS";
                defaultDescription = string.IsNullOrWhiteSpace(product.Name) ? product.SKU : product.Name;
                defaultPriceLcy = product.UnitPrice;
                break;
            }
            case ProjectLineKind.Expense:
            case ProjectLineKind.Text:
                if (string.IsNullOrWhiteSpace(dto.Description))
                    throw new ArgumentException($"Description is required for {dto.Kind} lines.");
                break;
        }

        line.Description = string.IsNullOrWhiteSpace(dto.Description) ? defaultDescription ?? string.Empty : dto.Description.Trim();
        line.UnitCostLcy = dto.UnitCostLcy ?? defaultCostLcy;
        line.UnitPrice = dto.UnitPrice ?? CurrencyMath.FromLcy(defaultPriceLcy, line.ExchangeRate);
        line.CurrencyCode = project.CurrencyCode;

        line.Recalculate();
        line.Validate();
    }

    private async Task<Project> GetProjectAsync(Guid projectId) =>
        await _projects.GetByIdAsync(projectId) ?? throw new NotFoundException(nameof(Project), projectId);

    private async Task EnsureLeafPostingTaskAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        var tasks = await _tasks.GetByProjectAsync(projectId, cancellationToken);
        var task = tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new ArgumentException($"Task {taskId} does not belong to this project.");
        if (task.TaskType != ProjectTaskType.Posting || ProjectTaskTree.HasChildren(tasks, task.Id))
            throw new InvalidOperationException($"Task {task.TaskNo} is not a leaf Posting task.");
    }
}

/// <summary>Currency conversion helpers (rate = LCY per 1 unit of foreign currency).</summary>
public static class CurrencyMath
{
    /// <summary>Converts an LCY amount to the foreign currency (4 decimals, for unit prices).</summary>
    public static decimal FromLcy(decimal amountLcy, decimal rate) =>
        rate <= 0 ? amountLcy : Math.Round(amountLcy / rate, 4, MidpointRounding.AwayFromZero);

    /// <summary>Converts a foreign-currency amount to LCY (2 decimals).</summary>
    public static decimal ToLcy(decimal amount, decimal rate) =>
        Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
}
