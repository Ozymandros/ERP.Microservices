using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using MyApp.Projects.Application.Contracts.DTOs;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Integration;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Entities;
using MyApp.Projects.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Projects.Application.Tests.Services;

/// <summary>Tests for <see cref="ProjectPostingService"/>: invoicing and consumption loops with Billing/Orders.</summary>
public class ProjectPostingServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IProjectTaskRepository> _tasks = new();
    private readonly Mock<IProjectPlanningLineRepository> _lines = new();
    private readonly Mock<IProjectLedgerEntryRepository> _ledger = new();
    private readonly Mock<IProjectInvoiceRequestRepository> _invoiceRequests = new();
    private readonly Mock<IProjectConsumptionOrderRepository> _consumptionOrders = new();
    private readonly Mock<IResourceRepository> _resources = new();
    private readonly Mock<ICurrencyService> _currency = new();
    private readonly Mock<IOrdersClient> _orders = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly List<ProjectLedgerEntry> _postedEntries = new();
    private readonly ProjectPostingService _sut;

    private readonly Project _project;
    private readonly ProjectTask _task;

    public ProjectPostingServiceTests()
    {
        _project = new Project(Guid.NewGuid())
        {
            ProjectNumber = "PRJ-2026-00001",
            CustomerId = Guid.NewGuid(),
            CurrencyCode = "USD",
            Status = ProjectStatus.Open
        };
        _task = new ProjectTask(Guid.NewGuid()) { ProjectId = _project.Id, TaskNo = "1000", TaskType = ProjectTaskType.Posting };

        _projects.Setup(r => r.GetByIdAsync(_project.Id)).ReturnsAsync(_project);
        _tasks.Setup(r => r.GetByProjectAsync(_project.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectTask> { _task });
        _ledger.Setup(r => r.GetMaxEntryNoAsync(_project.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => _postedEntries.Count);
        _ledger.Setup(r => r.AddAsync(It.IsAny<ProjectLedgerEntry>()))
               .Callback<ProjectLedgerEntry>(_postedEntries.Add)
               .Returns(Task.CompletedTask);
        _currency.Setup(c => c.GetRateAsync("USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0.9m);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<EntityEntryDto>());

        _sut = new ProjectPostingService(
            _projects.Object, _tasks.Object, _lines.Object, _ledger.Object, _invoiceRequests.Object,
            _consumptionOrders.Object, _resources.Object, _currency.Object, _orders.Object, _mapper.Object,
            _unitOfWork.Object, _publisher.Object, Mock.Of<ILogger<ProjectPostingService>>());
    }

    private ProjectPlanningLine BillableLine(decimal quantity, decimal unitPrice, PlanningLineType type = PlanningLineType.BudgetAndBillable)
    {
        var line = new ProjectPlanningLine(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            ProjectTaskId = _task.Id,
            LineNo = 10000,
            LineType = type,
            Kind = ProjectLineKind.Resource,
            ResourceId = Guid.NewGuid(),
            Description = "Consultant",
            Quantity = quantity,
            UnitCostLcy = 40m,
            UnitPrice = unitPrice,
            CurrencyCode = "USD",
            ExchangeRate = 0.9m
        };
        line.Recalculate();
        return line;
    }

    // ─── Invoicing ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RequestInvoiceAsync_AllBillableLines_TransfersQuantities_AndPublishesRequest()
    {
        var billable = BillableLine(7.5m, 100m);
        var budgetOnly = BillableLine(3m, 50m, PlanningLineType.Budget);
        _lines.Setup(r => r.GetByProjectAsync(_project.Id, null, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectPlanningLine> { billable, budgetOnly });
        ProjectInvoiceRequest? saved = null;
        _invoiceRequests.Setup(r => r.AddAsync(It.IsAny<ProjectInvoiceRequest>()))
                        .Callback<ProjectInvoiceRequest>(r => saved = r)
                        .Returns<ProjectInvoiceRequest>(r => Task.FromResult(r));

        await _sut.RequestInvoiceAsync(_project.Id, new CreateInvoiceRequestDto { TaxRate = 21m });

        billable.QuantityTransferredToInvoice.Should().Be(7.5m);
        budgetOnly.QuantityTransferredToInvoice.Should().Be(0m);
        saved!.Lines.Should().ContainSingle();
        saved.TotalAmount.Should().Be(750m);
        saved.CurrencyCode.Should().Be("USD");
        _publisher.Verify(p => p.PublishAsync(
            MessagingConstants.Topics.ProjectInvoiceRequested,
            It.Is<ProjectInvoiceRequestedEvent>(e =>
                e.RequestId == saved.Id &&
                e.CustomerId == _project.CustomerId &&
                e.Currency == "USD" &&
                e.Lines.Count == 1 &&
                e.Lines[0].Quantity == 7.5m &&
                e.Lines[0].UnitPrice == 100m &&
                e.Lines[0].TaxRate == 21m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestInvoiceAsync_PublishFails_RevertsTransfer()
    {
        var billable = BillableLine(2m, 100m);
        _lines.Setup(r => r.GetByProjectAsync(_project.Id, null, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectPlanningLine> { billable });
        _invoiceRequests.Setup(r => r.AddAsync(It.IsAny<ProjectInvoiceRequest>())).Returns<ProjectInvoiceRequest>(r => Task.FromResult(r));
        _publisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ProjectInvoiceRequestedEvent>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new Exception("broker down"));

        var act = () => _sut.RequestInvoiceAsync(_project.Id, new CreateInvoiceRequestDto());

        await act.Should().ThrowAsync<InvalidOperationException>();
        billable.QuantityTransferredToInvoice.Should().Be(0m);
    }

    [Fact]
    public async Task RequestInvoiceAsync_ProjectNotOpen_Throws()
    {
        _project.Status = ProjectStatus.Planning;

        var act = () => _sut.RequestInvoiceAsync(_project.Id, new CreateInvoiceRequestDto());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RequestInvoiceAsync_NothingToInvoice_Throws()
    {
        var line = BillableLine(1m, 10m);
        line.TransferToInvoice(1m);
        _lines.Setup(r => r.GetByProjectAsync(_project.Id, null, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectPlanningLine> { line });

        var act = () => _sut.RequestInvoiceAsync(_project.Id, new CreateInvoiceRequestDto());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private (ProjectInvoiceRequest Request, ProjectPlanningLine Line) ArrangeRequestedInvoice(decimal quantity = 4m)
    {
        var line = BillableLine(10m, 100m);
        line.TransferToInvoice(quantity);
        var request = new ProjectInvoiceRequest(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            CustomerId = _project.CustomerId,
            CurrencyCode = "USD",
            Status = ProjectInvoiceRequestStatus.Drafted
        };
        request.Lines.Add(new ProjectInvoiceRequestLine(Guid.NewGuid())
        {
            InvoiceRequestId = request.Id,
            PlanningLineId = line.Id,
            ProjectTaskId = _task.Id,
            Description = line.Description,
            Quantity = quantity,
            UnitPrice = 100m,
            LineAmount = quantity * 100m
        });
        _invoiceRequests.Setup(r => r.GetWithLinesAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _lines.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectPlanningLine> { line });
        return (request, line);
    }

    private InvoiceIssuedEvent IssuedEvent(Guid requestId, string? sourceType = DocumentSources.Project) =>
        new(Guid.NewGuid(), "INV-PRJ-1", _project.CustomerId, null, 400m, 0m, 400m, DateTime.UtcNow.AddDays(30),
            sourceType, requestId, "USD");

    [Fact]
    public async Task HandleInvoiceIssuedAsync_PostsSaleEntries_AndIsIdempotent()
    {
        var (request, line) = ArrangeRequestedInvoice(4m);
        var @event = IssuedEvent(request.Id);

        var first = await _sut.HandleInvoiceIssuedAsync(@event);
        var second = await _sut.HandleInvoiceIssuedAsync(@event);

        first.Should().Be(_project.Id);
        second.Should().Be(_project.Id);
        request.Status.Should().Be(ProjectInvoiceRequestStatus.Invoiced);
        request.InvoiceNumber.Should().Be("INV-PRJ-1");
        line.QuantityInvoiced.Should().Be(4m);
        _postedEntries.Should().ContainSingle();
        var entry = _postedEntries[0];
        entry.EntryType.Should().Be(ProjectLedgerEntryType.Sale);
        entry.LineAmount.Should().Be(400m);
        entry.LineAmountLcy.Should().Be(360m);
        entry.SourceDocumentId.Should().Be(@event.InvoiceId);
    }

    [Fact]
    public async Task HandleInvoiceIssuedAsync_SalesInvoice_IsIgnored()
    {
        var result = await _sut.HandleInvoiceIssuedAsync(IssuedEvent(Guid.NewGuid(), sourceType: null));

        result.Should().BeNull();
        _invoiceRequests.Verify(r => r.GetWithLinesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _postedEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleInvoiceCancelledAsync_AfterIssue_ReversesSaleAndReleasesQuantity()
    {
        var (request, line) = ArrangeRequestedInvoice(4m);
        var issued = IssuedEvent(request.Id);
        await _sut.HandleInvoiceIssuedAsync(issued);

        await _sut.HandleInvoiceCancelledAsync(new InvoiceCancelledEvent(
            issued.InvoiceId, issued.InvoiceNumber, "wrong rate", DocumentSources.Project, request.Id));

        request.Status.Should().Be(ProjectInvoiceRequestStatus.Cancelled);
        line.QuantityInvoiced.Should().Be(0m);
        line.QuantityTransferredToInvoice.Should().Be(0m);
        _postedEntries.Should().HaveCount(2);
        _postedEntries.Sum(e => e.LineAmount).Should().Be(0m);
    }

    [Fact]
    public async Task HandleInvoiceCancelledAsync_Draft_OnlyReleasesQuantity()
    {
        var (request, line) = ArrangeRequestedInvoice(4m);

        await _sut.HandleInvoiceCancelledAsync(new InvoiceCancelledEvent(
            Guid.NewGuid(), "DRAFT-1", "not needed", DocumentSources.Project, request.Id));

        line.QuantityTransferredToInvoice.Should().Be(0m);
        _postedEntries.Should().BeEmpty();
    }

    // ─── Item consumption ───────────────────────────────────────────────────

    private ProjectPlanningLine ItemLine(decimal quantity = 10m) => new(Guid.NewGuid())
    {
        ProjectId = _project.Id,
        ProjectTaskId = _task.Id,
        LineNo = 20000,
        Kind = ProjectLineKind.Item,
        ProductId = Guid.NewGuid(),
        Description = "Cable 3x2.5",
        Quantity = quantity,
        UnitCostLcy = 2m,
        UnitPrice = 5m,
        CurrencyCode = "USD",
        ExchangeRate = 0.9m
    };

    [Fact]
    public async Task CreateItemConsumptionAsync_CreatesProjectSourcedOutboundOrder_AndReservesQuantity()
    {
        var line = ItemLine();
        var warehouseId = Guid.NewGuid();
        _lines.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectPlanningLine> { line });
        CreateOrderWithReservationDto? sent = null;
        _orders.Setup(o => o.CreateOrderWithReservationAsync(It.IsAny<CreateOrderWithReservationDto>(), It.IsAny<CancellationToken>()))
               .Callback<CreateOrderWithReservationDto, CancellationToken>((dto, _) => sent = dto)
               .ReturnsAsync(new OrderDto(Guid.NewGuid()) { OrderNumber = "ORD-1" });
        _consumptionOrders.Setup(r => r.AddAsync(It.IsAny<ProjectConsumptionOrder>())).Returns<ProjectConsumptionOrder>(c => Task.FromResult(c));

        await _sut.CreateItemConsumptionAsync(_project.Id, new CreateItemConsumptionDto
        {
            WarehouseId = warehouseId,
            Lines = new List<ItemConsumptionLineDto> { new() { PlanningLineId = line.Id, Quantity = 6 } }
        });

        sent!.Type.Should().Be(OrderType.Outbound);
        sent.ExternalOrderId.Should().Be(_project.Id);
        sent.ExternalOrderSource.Should().Be(DocumentSources.Project);
        sent.WarehouseId.Should().Be(warehouseId);
        sent.Lines.Should().ContainSingle(l => l.ProductId == line.ProductId && l.Quantity == 6);
        line.QuantityInConsumption.Should().Be(6m);
    }

    [Fact]
    public async Task HandleOrderFulfilledAsync_ProjectOrder_PostsUsage_AndIsIdempotent()
    {
        var line = ItemLine();
        line.ReserveForConsumption(6m);
        var orderId = Guid.NewGuid();
        var consumption = new ProjectConsumptionOrder(Guid.NewGuid())
        {
            ProjectId = _project.Id,
            OrderId = orderId,
            OrderNumber = "ORD-1",
            Status = ProjectConsumptionStatus.Pending,
            PostingDate = new DateTime(2026, 11, 2)
        };
        consumption.Lines.Add(new ProjectConsumptionOrderLine(Guid.NewGuid())
        {
            ConsumptionOrderId = consumption.Id, PlanningLineId = line.Id, ProductId = line.ProductId!.Value, Quantity = 6
        });
        _consumptionOrders.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(consumption);
        _lines.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<ProjectPlanningLine> { line });

        var @event = new OrderFulfilledEvent(orderId, "ORD-1", OrderTypes.Outbound, Guid.NewGuid(), DateTime.UtcNow, null,
            _project.Id, new List<OrderLineEvent> { new(line.ProductId!.Value, 6) }, DocumentSources.Project);

        await _sut.HandleOrderFulfilledAsync(@event);
        await _sut.HandleOrderFulfilledAsync(@event);

        consumption.Status.Should().Be(ProjectConsumptionStatus.Fulfilled);
        line.QuantityPosted.Should().Be(6m);
        line.QuantityInConsumption.Should().Be(0m);
        _postedEntries.Should().ContainSingle();
        _postedEntries[0].EntryType.Should().Be(ProjectLedgerEntryType.Usage);
        _postedEntries[0].TotalCostLcy.Should().Be(12m);
        _postedEntries[0].PostingDate.Should().Be(new DateTime(2026, 11, 2));
    }

    [Fact]
    public async Task HandleOrderFulfilledAsync_SalesOrder_IsIgnored()
    {
        var @event = new OrderFulfilledEvent(Guid.NewGuid(), "ORD-2", OrderTypes.Outbound, Guid.NewGuid(), DateTime.UtcNow, null,
            Guid.NewGuid(), new List<OrderLineEvent>());

        var result = await _sut.HandleOrderFulfilledAsync(@event);

        result.Should().BeNull();
        _consumptionOrders.Verify(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Usage ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostUsageAsync_LinkedToPlanningLine_UsesLinePrices_AndUpdatesPostedQuantity()
    {
        var line = BillableLine(10m, 100m);
        line.LineDiscountPercent = 10m;
        line.Recalculate();
        _lines.Setup(r => r.GetByIdAsync(line.Id)).ReturnsAsync(line);
        _resources.Setup(r => r.GetByIdAsync(line.ResourceId!.Value))
                  .ReturnsAsync(new Resource(line.ResourceId!.Value) { ResourceNo = "R0001", Name = "Consultant", UnitCost = 99m });

        await _sut.PostUsageAsync(_project.Id, new PostUsageDto
        {
            ProjectTaskId = _task.Id,
            PlanningLineId = line.Id,
            Kind = ProjectLineKind.Resource,
            Quantity = 2.5m
        });

        line.QuantityPosted.Should().Be(2.5m);
        var entry = _postedEntries.Single();
        entry.UnitCostLcy.Should().Be(40m);   // from planning line, not resource card
        entry.UnitPrice.Should().Be(90m);     // net of 10 % line discount
        entry.TotalCostLcy.Should().Be(100m);
        entry.LineAmount.Should().Be(225m);
        entry.ExchangeRate.Should().Be(0.9m);
    }

    [Fact]
    public async Task PostUsageAsync_ItemKind_IsRejected()
    {
        var act = () => _sut.PostUsageAsync(_project.Id, new PostUsageDto
        {
            ProjectTaskId = _task.Id, Kind = ProjectLineKind.Item, Quantity = 1
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
