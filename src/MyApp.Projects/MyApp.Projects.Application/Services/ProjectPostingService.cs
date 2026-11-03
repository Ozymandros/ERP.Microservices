using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
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
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Projects.Application.Services;

/// <summary>
/// Posting operations (usage, item consumption through Orders, invoicing through Billing) and the
/// handlers for the integration events that close those loops. All event handlers are idempotent
/// (Dapr pub/sub delivers at least once).
/// </summary>
public class ProjectPostingService : AppServiceBase, IProjectPostingService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectTaskRepository _tasks;
    private readonly IProjectPlanningLineRepository _lines;
    private readonly IProjectLedgerEntryRepository _ledger;
    private readonly IProjectInvoiceRequestRepository _invoiceRequests;
    private readonly IProjectConsumptionOrderRepository _consumptionOrders;
    private readonly IResourceRepository _resources;
    private readonly ICurrencyService _currencyService;
    private readonly IOrdersClient _ordersClient;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectPostingService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ProjectPostingService"/> class.</summary>
    public ProjectPostingService(
        IProjectRepository projects,
        IProjectTaskRepository tasks,
        IProjectPlanningLineRepository lines,
        IProjectLedgerEntryRepository ledger,
        IProjectInvoiceRequestRepository invoiceRequests,
        IProjectConsumptionOrderRepository consumptionOrders,
        IResourceRepository resources,
        ICurrencyService currencyService,
        IOrdersClient ordersClient,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ProjectPostingService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Projects)
    {
        _projects = projects;
        _tasks = tasks;
        _lines = lines;
        _ledger = ledger;
        _invoiceRequests = invoiceRequests;
        _consumptionOrders = consumptionOrders;
        _resources = resources;
        _currencyService = currencyService;
        _ordersClient = ordersClient;
        _mapper = mapper;
        _logger = logger;
    }

    // ───────────────────────────── Ledger / usage ─────────────────────────────

    /// <inheritdoc />
    public async Task<List<ProjectLedgerEntryDto>> GetLedgerEntriesAsync(Guid projectId, Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        await GetProjectAsync(projectId);
        return _mapper.Map<List<ProjectLedgerEntryDto>>(await _ledger.GetByProjectAsync(projectId, taskId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ProjectLedgerEntryDto> PostUsageAsync(Guid projectId, PostUsageDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanPost();

        if (dto.Kind is not (ProjectLineKind.Resource or ProjectLineKind.Expense))
            throw new ArgumentException("Only Resource and Expense usage can be posted directly. Use item consumption for items.");
        if (dto.Quantity == 0)
            throw new ArgumentException("Quantity cannot be zero.");

        await EnsureLeafPostingTaskAsync(projectId, dto.ProjectTaskId, cancellationToken);

        ProjectPlanningLine? line = null;
        if (dto.PlanningLineId.HasValue)
        {
            line = await _lines.GetByIdAsync(dto.PlanningLineId.Value);
            if (line is null || line.ProjectId != projectId)
                throw new ArgumentException($"Planning line {dto.PlanningLineId} does not belong to this project.");
            if (line.ProjectTaskId != dto.ProjectTaskId)
                throw new ArgumentException("The planning line belongs to a different task.");
            if (line.Kind != dto.Kind)
                throw new ArgumentException($"The planning line is a {line.Kind} line.");
            if (dto.ResourceId.HasValue && line.ResourceId != dto.ResourceId)
                throw new ArgumentException("The planning line refers to a different resource.");
            if (line.QuantityPosted + dto.Quantity < 0)
                throw new InvalidOperationException("The correction exceeds the quantity posted on the planning line.");
        }

        Resource? resource = null;
        var resourceId = dto.ResourceId ?? line?.ResourceId;
        if (dto.Kind == ProjectLineKind.Resource)
        {
            if (resourceId is null)
                throw new ArgumentException("ResourceId is required for resource usage.");
            resource = await _resources.GetByIdAsync(resourceId.Value)
                ?? throw new ArgumentException($"Resource {resourceId} does not exist.");
            if (resource.Blocked)
                throw new InvalidOperationException($"Resource {resource.ResourceNo} is blocked.");
        }

        var postingDate = (dto.PostingDate ?? DateTime.UtcNow).Date;
        var rate = await _currencyService.GetRateAsync(project.CurrencyCode, postingDate, cancellationToken);

        var description = !string.IsNullOrWhiteSpace(dto.Description)
            ? dto.Description.Trim()
            : line?.Description ?? resource?.Name
              ?? throw new ArgumentException("Description is required for expense usage.");

        var entry = new ProjectLedgerEntry(Guid.NewGuid())
        {
            EntryNo = await _ledger.GetMaxEntryNoAsync(projectId, cancellationToken) + 1,
            EntryType = ProjectLedgerEntryType.Usage,
            PostingDate = postingDate,
            ProjectId = projectId,
            ProjectTaskId = dto.ProjectTaskId,
            PlanningLineId = line?.Id,
            Kind = dto.Kind,
            ResourceId = resource?.Id,
            Description = description,
            Quantity = dto.Quantity,
            UnitCostLcy = dto.UnitCostLcy ?? line?.UnitCostLcy ?? resource?.UnitCost ?? 0,
            UnitPrice = dto.UnitPrice ?? line?.NetUnitPrice ?? CurrencyMath.FromLcy(resource?.UnitPrice ?? 0, rate),
            CurrencyCode = project.CurrencyCode,
            ExchangeRate = rate,
            SourceDocument = ProjectSourceDocument.Manual
        };
        entry.Calculate();

        line?.RegisterUsage(dto.Quantity);

        await _ledger.AddAsync(entry);
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Posted usage entry {EntryNo} on project {ProjectNumber}: {Quantity} x {Kind}",
            entry.EntryNo, project.ProjectNumber, entry.Quantity, entry.Kind);

        return _mapper.Map<ProjectLedgerEntryDto>(entry);
    }

    // ───────────────────────────── Item consumption (Orders) ─────────────────────────────

    /// <inheritdoc />
    public async Task<ConsumptionOrderDto> CreateItemConsumptionAsync(Guid projectId, CreateItemConsumptionDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanPost();

        if (dto.Lines.Count == 0)
            throw new ArgumentException("At least one line is required.");

        var requested = dto.Lines
            .GroupBy(l => l.PlanningLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var lines = await _lines.GetByIdsAsync(requested.Keys, cancellationToken);
        if (lines.Count != requested.Count || lines.Any(l => l.ProjectId != projectId))
            throw new ArgumentException("One or more planning lines do not belong to this project.");

        foreach (var line in lines)
        {
            if (line.Kind != ProjectLineKind.Item || line.ProductId is null)
                throw new ArgumentException($"Planning line {line.LineNo} is not an item line.");
            if (line.WarehouseId.HasValue && line.WarehouseId != dto.WarehouseId)
                throw new ArgumentException($"Planning line {line.LineNo} is planned for a different warehouse.");

            line.ReserveForConsumption(requested[line.Id]);
        }

        // 1) Create the operational order in Orders (physical movement, reserves stock in Inventory).
        var orderRequest = new CreateOrderWithReservationDto
        {
            OrderNumber = $"{project.ProjectNumber}-CONS",
            Type = OrderType.Outbound,
            SourceId = dto.WarehouseId,
            TargetId = null,
            ExternalOrderId = project.Id,
            ExternalOrderSource = DocumentSources.Project,
            WarehouseId = dto.WarehouseId,
            OrderDate = DateTime.UtcNow,
            DestinationAddress = dto.DestinationAddress,
            Lines = lines.Select(l => new CreateOrderLineDto
            {
                ProductId = l.ProductId!.Value,
                Quantity = requested[l.Id]
            }).ToList()
        };

        var order = await _ordersClient.CreateOrderWithReservationAsync(orderRequest, cancellationToken);

        // 2) Track it locally so the fulfilment event can be turned into usage entries.
        var consumption = new ProjectConsumptionOrder(Guid.NewGuid())
        {
            ProjectId = projectId,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            WarehouseId = dto.WarehouseId,
            Status = ProjectConsumptionStatus.Pending,
            PostingDate = (dto.PostingDate ?? DateTime.UtcNow).Date
        };
        foreach (var line in lines)
        {
            consumption.Lines.Add(new ProjectConsumptionOrderLine(Guid.NewGuid())
            {
                ConsumptionOrderId = consumption.Id,
                PlanningLineId = line.Id,
                ProductId = line.ProductId!.Value,
                Quantity = requested[line.Id]
            });
        }

        try
        {
            await _consumptionOrders.AddAsync(consumption);
            await SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "Order {OrderId} was created in Orders for project {ProjectNumber} but could not be tracked locally; cancel it in Orders",
                order.Id, project.ProjectNumber);
            throw;
        }

        _logger.LogInformation(
            "Consumption order {OrderNumber} ({OrderId}) created for project {ProjectNumber}",
            order.OrderNumber, order.Id, project.ProjectNumber);

        return _mapper.Map<ConsumptionOrderDto>(consumption);
    }

    /// <inheritdoc />
    public async Task<List<ConsumptionOrderDto>> GetConsumptionOrdersAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await GetProjectAsync(projectId);
        return _mapper.Map<List<ConsumptionOrderDto>>(await _consumptionOrders.GetByProjectAsync(projectId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Guid?> HandleOrderFulfilledAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken = default)
    {
        if (!DocumentSources.IsProject(@event.ExternalOrderSource))
            return null;

        var consumption = await _consumptionOrders.GetByOrderIdAsync(@event.OrderId, cancellationToken);
        if (consumption is null)
        {
            _logger.LogWarning("Fulfilled order {OrderId} is not a known project consumption order", @event.OrderId);
            return null;
        }

        if (consumption.Status != ProjectConsumptionStatus.Pending)
        {
            _logger.LogInformation("Consumption order {OrderId} already {Status}; ignoring", @event.OrderId, consumption.Status);
            return consumption.ProjectId;
        }

        var project = await GetProjectAsync(consumption.ProjectId);
        var lines = (await _lines.GetByIdsAsync(consumption.Lines.Select(l => l.PlanningLineId), cancellationToken))
            .ToDictionary(l => l.Id);
        var postingDate = consumption.PostingDate == default ? @event.FulfilledDate.Date : consumption.PostingDate;
        var nextEntryNo = await _ledger.GetMaxEntryNoAsync(project.Id, cancellationToken);

        foreach (var consumptionLine in consumption.Lines)
        {
            if (!lines.TryGetValue(consumptionLine.PlanningLineId, out var line))
            {
                _logger.LogError("Planning line {PlanningLineId} of consumption order {OrderId} no longer exists",
                    consumptionLine.PlanningLineId, @event.OrderId);
                continue;
            }

            var rate = await GetRateOrFallbackAsync(project.CurrencyCode, postingDate, line.ExchangeRate, cancellationToken);
            var entry = new ProjectLedgerEntry(Guid.NewGuid())
            {
                EntryNo = ++nextEntryNo,
                EntryType = ProjectLedgerEntryType.Usage,
                PostingDate = postingDate,
                ProjectId = project.Id,
                ProjectTaskId = line.ProjectTaskId,
                PlanningLineId = line.Id,
                Kind = ProjectLineKind.Item,
                ProductId = consumptionLine.ProductId,
                Description = line.Description,
                Quantity = consumptionLine.Quantity,
                UnitCostLcy = line.UnitCostLcy,
                UnitPrice = line.NetUnitPrice,
                CurrencyCode = project.CurrencyCode,
                ExchangeRate = rate,
                SourceDocument = ProjectSourceDocument.ConsumptionOrder,
                SourceDocumentId = @event.OrderId,
                SourceDocumentNo = @event.OrderNumber
            };
            entry.Calculate();
            await _ledger.AddAsync(entry);

            line.ConfirmConsumption(consumptionLine.Quantity);
        }

        consumption.Status = ProjectConsumptionStatus.Fulfilled;
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Consumption order {OrderNumber} fulfilled; usage posted on project {ProjectNumber}",
            @event.OrderNumber, project.ProjectNumber);
        return project.Id;
    }

    /// <inheritdoc />
    public async Task<Guid?> HandleOrderCancelledAsync(OrderCancelledEvent @event, CancellationToken cancellationToken = default)
    {
        if (!DocumentSources.IsProject(@event.ExternalOrderSource))
            return null;

        var consumption = await _consumptionOrders.GetByOrderIdAsync(@event.OrderId, cancellationToken);
        if (consumption is null || consumption.Status != ProjectConsumptionStatus.Pending)
            return consumption?.ProjectId;

        var lines = (await _lines.GetByIdsAsync(consumption.Lines.Select(l => l.PlanningLineId), cancellationToken))
            .ToDictionary(l => l.Id);
        foreach (var consumptionLine in consumption.Lines)
        {
            if (lines.TryGetValue(consumptionLine.PlanningLineId, out var line))
                line.ReleaseConsumption(consumptionLine.Quantity);
        }

        consumption.Status = ProjectConsumptionStatus.Cancelled;
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Consumption order {OrderId} cancelled ({Reason}); quantities released", @event.OrderId, @event.Reason);
        return consumption.ProjectId;
    }

    // ───────────────────────────── Invoicing (Billing) ─────────────────────────────

    /// <inheritdoc />
    public async Task<InvoiceRequestDto> RequestInvoiceAsync(Guid projectId, CreateInvoiceRequestDto dto, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(projectId);
        project.EnsureCanPost();

        var allLines = await _lines.GetByProjectAsync(projectId, cancellationToken: cancellationToken);
        List<(ProjectPlanningLine Line, decimal Quantity)> selection;

        if (dto.Lines.Count == 0)
        {
            selection = allLines.Where(l => l.QuantityToInvoice > 0).Select(l => (l, l.QuantityToInvoice)).ToList();
        }
        else
        {
            var byId = allLines.ToDictionary(l => l.Id);
            selection = new List<(ProjectPlanningLine, decimal)>();
            foreach (var input in dto.Lines.GroupBy(l => l.PlanningLineId))
            {
                if (!byId.TryGetValue(input.Key, out var line))
                    throw new ArgumentException($"Planning line {input.Key} does not belong to this project.");
                if (!line.IsBillable)
                    throw new ArgumentException($"Planning line {line.LineNo} is not billable.");

                var quantity = input.Any(i => i.Quantity is null)
                    ? line.QuantityToInvoice
                    : input.Sum(i => i.Quantity!.Value);
                if (quantity > 0)
                    selection.Add((line, quantity));
            }
        }

        if (selection.Count == 0)
            throw new InvalidOperationException("There is nothing to invoice for this project.");

        var request = new ProjectInvoiceRequest(Guid.NewGuid())
        {
            ProjectId = project.Id,
            CustomerId = project.CustomerId,
            CurrencyCode = project.CurrencyCode,
            Status = ProjectInvoiceRequestStatus.Requested,
            RequestedAt = DateTime.UtcNow
        };

        foreach (var (line, quantity) in selection)
        {
            line.TransferToInvoice(quantity);
            var unitPrice = line.NetUnitPrice;
            request.Lines.Add(new ProjectInvoiceRequestLine(Guid.NewGuid())
            {
                InvoiceRequestId = request.Id,
                PlanningLineId = line.Id,
                ProjectTaskId = line.ProjectTaskId,
                Description = line.Description,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TaxRate = dto.TaxRate,
                LineAmount = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero)
            });
        }

        request.TotalAmount = request.Lines.Sum(l => l.LineAmount);

        await _invoiceRequests.AddAsync(request);
        await SaveChangesAsync(cancellationToken);

        var @event = new ProjectInvoiceRequestedEvent(
            request.Id,
            project.Id,
            project.ProjectNumber,
            project.CustomerId,
            project.CurrencyCode,
            request.Lines.Select(l => new ProjectInvoiceLineEvent(
                l.PlanningLineId,
                $"{project.ProjectNumber} - {l.Description}",
                l.Quantity,
                l.UnitPrice,
                0m,
                l.TaxRate)).ToList());

        try
        {
            await EventPublisher.PublishAsync(MessagingConstants.Topics.ProjectInvoiceRequested, @event, cancellationToken);
        }
        catch (Exception ex)
        {
            // Compensate: Billing never received the request, so release the quantities again.
            _logger.LogError(ex, "Failed to publish invoice request {RequestId}; reverting", request.Id);
            foreach (var (line, quantity) in selection)
                line.ReverseTransferToInvoice(quantity);
            request.Status = ProjectInvoiceRequestStatus.Cancelled;
            await SaveChangesAsync(CancellationToken.None);
            throw new InvalidOperationException("The invoice request could not be sent to Billing. Please retry.", ex);
        }

        _logger.LogInformation(
            "Invoice request {RequestId} for project {ProjectNumber} sent to Billing ({Total} {Currency})",
            request.Id, project.ProjectNumber, request.TotalAmount, request.CurrencyCode);

        return _mapper.Map<InvoiceRequestDto>(request);
    }

    /// <inheritdoc />
    public async Task<List<InvoiceRequestDto>> GetInvoiceRequestsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await GetProjectAsync(projectId);
        return _mapper.Map<List<InvoiceRequestDto>>(await _invoiceRequests.GetByProjectAsync(projectId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Guid?> HandleInvoiceCreatedAsync(InvoiceCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var request = await GetProjectInvoiceRequestAsync(@event.SourceType, @event.SourceId, cancellationToken);
        if (request is null)
            return null;

        if (request.Status == ProjectInvoiceRequestStatus.Requested)
        {
            request.InvoiceId = @event.InvoiceId;
            request.Status = ProjectInvoiceRequestStatus.Drafted;
            await SaveChangesAsync(cancellationToken);
        }

        return request.ProjectId;
    }

    /// <inheritdoc />
    public async Task<Guid?> HandleInvoiceIssuedAsync(InvoiceIssuedEvent @event, CancellationToken cancellationToken = default)
    {
        var request = await GetProjectInvoiceRequestAsync(@event.SourceType, @event.SourceId, cancellationToken);
        if (request is null)
            return null;

        if (request.Status is ProjectInvoiceRequestStatus.Invoiced or ProjectInvoiceRequestStatus.Cancelled)
        {
            _logger.LogInformation("Invoice request {RequestId} already {Status}; ignoring issue event", request.Id, request.Status);
            return request.ProjectId;
        }

        var project = await GetProjectAsync(request.ProjectId);
        var postingDate = DateTime.UtcNow.Date;
        await PostSaleEntriesAsync(project, request, @event.InvoiceId, @event.InvoiceNumber, postingDate, sign: 1, cancellationToken);

        request.InvoiceId = @event.InvoiceId;
        request.InvoiceNumber = @event.InvoiceNumber;
        request.Status = ProjectInvoiceRequestStatus.Invoiced;
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice {InvoiceNumber} issued for project {ProjectNumber}; sale entries posted",
            @event.InvoiceNumber, project.ProjectNumber);
        return project.Id;
    }

    /// <inheritdoc />
    public async Task<Guid?> HandleInvoiceCancelledAsync(InvoiceCancelledEvent @event, CancellationToken cancellationToken = default)
    {
        var request = await GetProjectInvoiceRequestAsync(@event.SourceType, @event.SourceId, cancellationToken);
        if (request is null || request.Status == ProjectInvoiceRequestStatus.Cancelled)
            return request?.ProjectId;

        var project = await GetProjectAsync(request.ProjectId);
        var lines = (await _lines.GetByIdsAsync(request.Lines.Select(l => l.PlanningLineId), cancellationToken))
            .ToDictionary(l => l.Id);

        if (request.Status == ProjectInvoiceRequestStatus.Invoiced)
        {
            // Reverse the sale entries posted when the invoice was issued.
            await PostSaleEntriesAsync(project, request, @event.InvoiceId, @event.InvoiceNumber, DateTime.UtcNow.Date, sign: -1, cancellationToken);
        }

        foreach (var requestLine in request.Lines)
        {
            if (lines.TryGetValue(requestLine.PlanningLineId, out var line))
                line.ReverseTransferToInvoice(requestLine.Quantity);
        }

        request.Status = ProjectInvoiceRequestStatus.Cancelled;
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice {InvoiceNumber} cancelled; project {ProjectNumber} quantities released",
            @event.InvoiceNumber, project.ProjectNumber);
        return project.Id;
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    private async Task PostSaleEntriesAsync(
        Project project,
        ProjectInvoiceRequest request,
        Guid invoiceId,
        string invoiceNumber,
        DateTime postingDate,
        int sign,
        CancellationToken cancellationToken)
    {
        var lines = (await _lines.GetByIdsAsync(request.Lines.Select(l => l.PlanningLineId), cancellationToken))
            .ToDictionary(l => l.Id);
        var nextEntryNo = await _ledger.GetMaxEntryNoAsync(project.Id, cancellationToken);

        foreach (var requestLine in request.Lines)
        {
            lines.TryGetValue(requestLine.PlanningLineId, out var line);
            var rate = await GetRateOrFallbackAsync(request.CurrencyCode, postingDate, line?.ExchangeRate ?? 1m, cancellationToken);

            var entry = new ProjectLedgerEntry(Guid.NewGuid())
            {
                EntryNo = ++nextEntryNo,
                EntryType = ProjectLedgerEntryType.Sale,
                PostingDate = postingDate,
                ProjectId = project.Id,
                ProjectTaskId = requestLine.ProjectTaskId,
                PlanningLineId = requestLine.PlanningLineId,
                Kind = line?.Kind ?? ProjectLineKind.Expense,
                ResourceId = line?.ResourceId,
                ProductId = line?.ProductId,
                Description = requestLine.Description,
                Quantity = sign * requestLine.Quantity,
                UnitPrice = requestLine.UnitPrice,
                CurrencyCode = request.CurrencyCode,
                ExchangeRate = rate,
                SourceDocument = ProjectSourceDocument.Invoice,
                SourceDocumentId = invoiceId,
                SourceDocumentNo = invoiceNumber
            };
            entry.Calculate();
            await _ledger.AddAsync(entry);

            line?.RegisterInvoiced(sign * requestLine.Quantity);
        }
    }

    private async Task<ProjectInvoiceRequest?> GetProjectInvoiceRequestAsync(string? sourceType, Guid? sourceId, CancellationToken cancellationToken)
    {
        if (!DocumentSources.IsProject(sourceType) || sourceId is null)
            return null;

        var request = await _invoiceRequests.GetWithLinesAsync(sourceId.Value, cancellationToken);
        if (request is null)
            _logger.LogWarning("Billing event references unknown project invoice request {RequestId}", sourceId);
        return request;
    }

    private async Task<decimal> GetRateOrFallbackAsync(string currencyCode, DateTime date, decimal fallback, CancellationToken cancellationToken)
    {
        try
        {
            return await _currencyService.GetRateAsync(currencyCode, date, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            // Never fail an integration event because of a missing rate: fall back to the planning rate.
            _logger.LogWarning(ex, "Using fallback exchange rate {Rate} for {Currency} on {Date:yyyy-MM-dd}", fallback, currencyCode, date);
            return fallback;
        }
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
