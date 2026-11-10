using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Integration;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Projects.Domain.Services;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Projects.Application.Services;

/// <summary>Project header operations, status lifecycle, copy and statistics.</summary>
public class ProjectService : AppServiceBase, IProjectService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectTaskRepository _tasks;
    private readonly IProjectPlanningLineRepository _planningLines;
    private readonly IProjectLedgerEntryRepository _ledger;
    private readonly ICurrencyService _currencyService;
    private readonly ISalesCustomerClient _customers;
    private readonly ProjectsSettings _settings;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProjectService"/> class.</summary>
    public ProjectService(
        IProjectRepository projects,
        IProjectTaskRepository tasks,
        IProjectPlanningLineRepository planningLines,
        IProjectLedgerEntryRepository ledger,
        ICurrencyService currencyService,
        ISalesCustomerClient customers,
        ProjectsSettings settings,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ProjectService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Projects)
    {
        _projects = projects;
        _tasks = tasks;
        _planningLines = planningLines;
        _ledger = ledger;
        _currencyService = currencyService;
        _customers = customers;
        _settings = settings;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProjectDto>> ListAsync(CancellationToken cancellationToken = default) =>
        _mapper.Map<List<ProjectDto>>(await _projects.ListAsync(cancellationToken));

    /// <inheritdoc />
    public async Task<PaginatedResult<ProjectDto>> QueryAsync(ISpecification<Project> spec, CancellationToken cancellationToken = default)
    {
        var result = await _projects.QueryAsync(spec);
        return new PaginatedResult<ProjectDto>(
            _mapper.Map<List<ProjectDto>>(result.Items), result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <inheritdoc />
    public async Task<ProjectDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(id);
        return project is null ? null : _mapper.Map<ProjectDto>(project);
    }

    /// <inheritdoc />
    public async Task<ProjectDto?> GetByNumberAsync(string projectNumber, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByNumberAsync(projectNumber, cancellationToken);
        return project is null ? null : _mapper.Map<ProjectDto>(project);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureCustomerExistsAsync(dto.CustomerId, cancellationToken);
        var currency = await EnsureCurrencyUsableAsync(dto.CurrencyCode, cancellationToken);

        var project = new Project(Guid.NewGuid())
        {
            ProjectNumber = await GenerateProjectNumberAsync(cancellationToken),
            Description = dto.Description.Trim(),
            CustomerId = dto.CustomerId,
            ProjectManagerUserId = dto.ProjectManagerUserId,
            StartingDate = dto.StartingDate,
            EndingDate = dto.EndingDate,
            CurrencyCode = currency,
            ExternalReference = dto.ExternalReference,
            Status = ProjectStatus.Planning
        };
        project.ValidateDates();

        await _projects.AddAsync(project);
        await SaveChangesAsync(cancellationToken);

        await PublishSafeAsync(
            MessagingConstants.Topics.ProjectCreated,
            new ProjectCreatedEvent(project.Id, project.ProjectNumber, project.CustomerId, project.CurrencyCode),
            cancellationToken);

        _logger.LogInformation("Project {ProjectNumber} created ({ProjectId})", project.ProjectNumber, project.Id);
        return _mapper.Map<ProjectDto>(project);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(id);

        // Blocked can always be changed (to unblock a project); everything else requires a modifiable project.
        var onlyBlockedChanged =
            project.Description == dto.Description.Trim() &&
            project.CustomerId == dto.CustomerId &&
            project.ProjectManagerUserId == dto.ProjectManagerUserId &&
            project.StartingDate == dto.StartingDate &&
            project.EndingDate == dto.EndingDate &&
            string.Equals(project.CurrencyCode, dto.CurrencyCode, StringComparison.OrdinalIgnoreCase) &&
            project.ExternalReference == dto.ExternalReference;
        if (!onlyBlockedChanged)
        {
            if (project.Status == ProjectStatus.Completed)
                throw new InvalidOperationException($"Project {project.ProjectNumber} is completed and cannot be modified.");
        }

        if (project.CustomerId != dto.CustomerId)
            await EnsureCustomerExistsAsync(dto.CustomerId, cancellationToken);

        var currency = await EnsureCurrencyUsableAsync(dto.CurrencyCode, cancellationToken);
        if (currency != project.CurrencyCode)
        {
            var hasLines = (await _planningLines.GetByProjectAsync(project.Id, cancellationToken: cancellationToken)).Count > 0;
            if (hasLines || await _ledger.AnyForProjectAsync(project.Id, cancellationToken))
                throw new InvalidOperationException(
                    "The project currency cannot be changed once planning lines or ledger entries exist.");
        }

        project.Description = dto.Description.Trim();
        project.CustomerId = dto.CustomerId;
        project.ProjectManagerUserId = dto.ProjectManagerUserId;
        project.StartingDate = dto.StartingDate;
        project.EndingDate = dto.EndingDate;
        project.CurrencyCode = currency;
        project.Blocked = dto.Blocked;
        project.ExternalReference = dto.ExternalReference;
        project.ValidateDates();

        await SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectDto>(project);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(id);

        if (project.Status is not (ProjectStatus.Planning or ProjectStatus.Quote))
            throw new InvalidOperationException("Only projects in Planning or Quote status can be deleted.");
        if (await _ledger.AnyForProjectAsync(project.Id, cancellationToken))
            throw new InvalidOperationException("Projects with ledger entries cannot be deleted.");

        // Planning lines and tasks are removed by the database cascade (Project -> Tasks / PlanningLines).
        await _projects.DeleteAsync(project);
        await SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Project {ProjectNumber} deleted", project.ProjectNumber);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> ChangeStatusAsync(Guid id, ProjectStatus status, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(id);
        var oldStatus = project.Status;
        if (oldStatus == status)
            return _mapper.Map<ProjectDto>(project);

        project.ChangeStatus(status);
        await SaveChangesAsync(cancellationToken);

        await PublishSafeAsync(
            MessagingConstants.Topics.ProjectStatusChanged,
            new ProjectStatusChangedEvent(project.Id, project.ProjectNumber, oldStatus.ToString(), status.ToString()),
            cancellationToken);

        return _mapper.Map<ProjectDto>(project);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> CopyAsync(Guid id, CopyProjectDto dto, CancellationToken cancellationToken = default)
    {
        var source = await GetProjectAsync(id);
        var customerId = dto.CustomerId ?? source.CustomerId;
        if (customerId != source.CustomerId)
            await EnsureCustomerExistsAsync(customerId, cancellationToken);

        var copy = new Project(Guid.NewGuid())
        {
            ProjectNumber = await GenerateProjectNumberAsync(cancellationToken),
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? Truncate($"Copy of {source.Description}", 250)
                : dto.Description.Trim(),
            CustomerId = customerId,
            ProjectManagerUserId = source.ProjectManagerUserId,
            StartingDate = source.StartingDate,
            EndingDate = source.EndingDate,
            CurrencyCode = source.CurrencyCode,
            ExternalReference = source.ExternalReference,
            Status = ProjectStatus.Planning
        };
        await _projects.AddAsync(copy);

        var sourceTasks = await _tasks.GetByProjectAsync(source.Id, cancellationToken);
        var idMap = sourceTasks.ToDictionary(t => t.Id, _ => Guid.NewGuid());
        foreach (var task in sourceTasks)
        {
            await _tasks.AddAsync(new ProjectTask(idMap[task.Id])
            {
                ProjectId = copy.Id,
                ParentTaskId = task.ParentTaskId.HasValue ? idMap[task.ParentTaskId.Value] : null,
                TaskNo = task.TaskNo,
                Description = task.Description,
                TaskType = task.TaskType,
                SortOrder = task.SortOrder,
                StartDate = task.StartDate,
                EndDate = task.EndDate
            });
        }

        if (dto.IncludePlanningLines)
        {
            var lines = await _planningLines.GetByProjectAsync(source.Id, cancellationToken: cancellationToken);
            foreach (var line in lines)
            {
                var newLine = new ProjectPlanningLine(Guid.NewGuid())
                {
                    ProjectId = copy.Id,
                    ProjectTaskId = idMap[line.ProjectTaskId],
                    LineNo = line.LineNo,
                    LineType = line.LineType,
                    Kind = line.Kind,
                    ResourceId = line.ResourceId,
                    ProductId = line.ProductId,
                    WarehouseId = line.WarehouseId,
                    Description = line.Description,
                    UnitOfMeasure = line.UnitOfMeasure,
                    PlanningDate = line.PlanningDate,
                    Quantity = line.Quantity,
                    UnitCostLcy = line.UnitCostLcy,
                    UnitPrice = line.UnitPrice,
                    LineDiscountPercent = line.LineDiscountPercent,
                    CurrencyCode = line.CurrencyCode,
                    ExchangeRate = line.ExchangeRate
                };
                newLine.Recalculate();
                await _planningLines.AddAsync(newLine);
            }
        }

        await SaveChangesAsync(cancellationToken);

        await PublishSafeAsync(
            MessagingConstants.Topics.ProjectCreated,
            new ProjectCreatedEvent(copy.Id, copy.ProjectNumber, copy.CustomerId, copy.CurrencyCode),
            cancellationToken);

        _logger.LogInformation("Project {Source} copied to {Copy}", source.ProjectNumber, copy.ProjectNumber);
        return _mapper.Map<ProjectDto>(copy);
    }

    /// <inheritdoc />
    public async Task<ProjectStatisticsDto> GetStatisticsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(id);
        var tasks = await _tasks.GetByProjectAsync(project.Id, cancellationToken);
        var lines = await _planningLines.GetByProjectAsync(project.Id, cancellationToken: cancellationToken);
        var entries = await _ledger.GetByProjectAsync(project.Id, cancellationToken: cancellationToken);

        var own = ProjectStatisticsCalculator.CalculateOwnAmounts(lines, entries);
        var rolledUp = ProjectTaskTree.RollUp<ProjectAmountsDto>(tasks, own, (a, b) => a + b, () => new ProjectAmountsDto());

        var totals = own.Values.Aggregate(new ProjectAmountsDto(), (acc, a) => acc + a);

        return new ProjectStatisticsDto
        {
            ProjectId = project.Id,
            CurrencyCode = project.CurrencyCode,
            LocalCurrency = _currencyService.LocalCurrency,
            Totals = totals,
            Tasks = tasks.Select(t => new TaskStatisticsDto
            {
                TaskId = t.Id,
                ParentTaskId = t.ParentTaskId,
                TaskNo = t.TaskNo,
                Description = t.Description,
                TaskType = t.TaskType.ToString(),
                Amounts = rolledUp.TryGetValue(t.Id, out var amounts) ? amounts : new ProjectAmountsDto()
            }).ToList()
        };
    }

    private async Task<Project> GetProjectAsync(Guid id) =>
        await _projects.GetByIdAsync(id) ?? throw new NotFoundException(nameof(Project), id);

    private async Task EnsureCustomerExistsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.");
        if (await _customers.GetCustomerAsync(customerId, cancellationToken) is null)
            throw new ArgumentException($"Customer {customerId} does not exist in Sales.");
    }

    private async Task<string> EnsureCurrencyUsableAsync(string? currencyCode, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode)
            ? _currencyService.LocalCurrency
            : currencyCode.Trim().ToUpperInvariant();

        if (code == _currencyService.LocalCurrency)
            return code;

        if (await _currencyService.GetByCodeAsync(code, cancellationToken) is null)
            throw new ArgumentException($"Currency {code} is not defined. Create it under /api/projects/currencies.");

        // Fail early when no exchange rate exists yet (needed for every planning line).
        await _currencyService.GetRateAsync(code, DateTime.UtcNow, cancellationToken);
        return code;
    }

    private async Task<string> GenerateProjectNumberAsync(CancellationToken cancellationToken)
    {
        var prefix = $"{_settings.NumberPrefix}-{DateTime.UtcNow:yyyy}-";
        var next = await _projects.CountByNumberPrefixAsync(prefix, cancellationToken) + 1;
        string number;
        do
        {
            number = $"{prefix}{next:00000}";
            next++;
        }
        while (await _projects.GetByNumberAsync(number, cancellationToken) is not null);

        return number;
    }

    private async Task PublishSafeAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            await EventPublisher.PublishAsync(topic, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {Topic}", topic);
        }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}

/// <summary>Computes per-task own amounts from planning lines and ledger entries.</summary>
public static class ProjectStatisticsCalculator
{
    /// <summary>Calculates the amounts of each task (not rolled up).</summary>
    /// <param name="lines">Planning lines.</param>
    /// <param name="entries">Ledger entries.</param>
    public static Dictionary<Guid, ProjectAmountsDto> CalculateOwnAmounts(
        IEnumerable<ProjectPlanningLine> lines,
        IEnumerable<ProjectLedgerEntry> entries)
    {
        var result = new Dictionary<Guid, ProjectAmountsDto>();

        void Add(Guid taskId, ProjectAmountsDto amounts) =>
            result[taskId] = result.TryGetValue(taskId, out var existing) ? existing + amounts : amounts;

        foreach (var line in lines)
        {
            Add(line.ProjectTaskId, new ProjectAmountsDto
            {
                BudgetCostLcy = line.IsBudget ? line.TotalCostLcy : 0,
                BudgetPrice = line.IsBudget ? line.LineAmount : 0,
                BillablePrice = line.IsBillable ? line.LineAmount : 0
            });
        }

        foreach (var entry in entries)
        {
            Add(entry.ProjectTaskId, entry.EntryType == ProjectLedgerEntryType.Usage
                ? new ProjectAmountsDto { UsageCostLcy = entry.TotalCostLcy, UsagePrice = entry.LineAmount }
                : new ProjectAmountsDto { InvoicedPrice = entry.LineAmount, InvoicedPriceLcy = entry.LineAmountLcy });
        }

        return result;
    }
}
