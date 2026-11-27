using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Application.UseCases.PurchaseOrders;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Purchasing.Application.Services;

/// <summary>Application service that implements purchase order business workflows including approval and receiving.</summary>
public class PurchaseOrderService : AppServiceBase, IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseOrderLineRepository _lineRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PurchaseOrderService> _logger;
    private readonly ReceivePurchaseOrderUseCase _receivePurchaseOrder;
    private readonly MatchPurchaseOrderUseCase _matchPurchaseOrder;

    /// <summary>
    /// Initializes a new instance of the PurchaseOrderService class.
    /// </summary>
    /// <param name="purchaseOrderRepository">The purchase Order Repository.</param>
    /// <param name="lineRepository">The line Repository.</param>
    /// <param name="supplierRepository">The supplier Repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="receivePurchaseOrder">The receive Purchase Order use case.</param>
    /// <param name="matchPurchaseOrder">The match Purchase Order use case.</param>
    public PurchaseOrderService(
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseOrderLineRepository lineRepository,
        ISupplierRepository supplierRepository,
        IMapper mapper,
        ILogger<PurchaseOrderService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ReceivePurchaseOrderUseCase receivePurchaseOrder,
        MatchPurchaseOrderUseCase matchPurchaseOrder)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Purchasing)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _lineRepository = lineRepository;
        _supplierRepository = supplierRepository;
        _mapper = mapper;
        _logger = logger;
        _receivePurchaseOrder = receivePurchaseOrder;
        _matchPurchaseOrder = matchPurchaseOrder;
    }

    /// <summary>
    /// Gets the purchase order by id asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id, cancellationToken);
        return order == null ? null : _mapper.Map<PurchaseOrderDto>(order);
    }

    /// <summary>
    /// Gets the purchase order by order number asynchronously.
    /// </summary>
    /// <param name="orderNumber">The order Number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<PurchaseOrderDto?> GetPurchaseOrderByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _purchaseOrderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
        return order == null ? null : _mapper.Map<PurchaseOrderDto>(order);
    }

    /// <summary>
    /// Gets all purchase orders asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _purchaseOrderRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    /// <summary>
    /// Gets the purchase orders by supplier asynchronously.
    /// </summary>
    /// <param name="supplierId">The supplier Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        var orders = await _purchaseOrderRepository.GetBySuppliersIdAsync(supplierId, cancellationToken);
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    /// <summary>
    /// Gets the purchase orders by status asynchronously.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        var orders = await _purchaseOrderRepository.GetByStatusAsync(status, cancellationToken);
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    /// <summary>
    /// Creates a purchase order asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreateUpdatePurchaseOrderDto dto)
    {
        // Validate supplier exists
        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
        if (supplier == null)
        {
            throw new KeyNotFoundException($"Supplier with ID '{dto.SupplierId}' not found.");
        }

        var order = _mapper.Map<PurchaseOrder>(dto);
        order.Id = Guid.NewGuid();
        order.OrderNumber = await GenerateOrderNumberAsync();
        order.Status = (PurchaseOrderStatus)dto.Status;

        // Calculate total amount from lines
        if (order.Lines.Count() != 0)
        {
            order.TotalAmount = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        }

        var createdOrder = await _purchaseOrderRepository.AddAsync(order);
        await SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(createdOrder);
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        // Example: Use a timestamp and a random suffix for uniqueness (replace with a DB sequence or other logic as needed)
        var now = DateTime.UtcNow;
        var random = Guid.NewGuid().ToString()[..8];
        var count = (await _purchaseOrderRepository.GetAllAsync()).Count() + 1; // Not perfect for concurrency, but placeholder
        return $"PO-{now:yyyyMMddHHmmss}-{count}-{random}";
    }

    /// <summary>
    /// Updates the purchase order asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<PurchaseOrderDto> UpdatePurchaseOrderAsync(Guid id, CreateUpdatePurchaseOrderDto dto)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{id}' not found.");
        }

        // Validate supplier exists if changed
        if (order.SupplierId != dto.SupplierId)
        {
            var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId);
            if (supplier == null)
            {
                throw new KeyNotFoundException($"Supplier with ID '{dto.SupplierId}' not found.");
            }
        }

        _mapper.Map(dto, order);
        order.Status = (PurchaseOrderStatus)dto.Status;

        // Recalculate total amount from lines
        if (order.Lines.Count() != 0)
        {
            order.TotalAmount = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        }

        var updatedOrder = await _purchaseOrderRepository.UpdateAsync(order);
        await SaveChangesAsync();
        return _mapper.Map<PurchaseOrderDto>(updatedOrder);
    }

    /// <summary>
    /// Updates the purchase order status asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="status">The status.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<PurchaseOrderDto> UpdatePurchaseOrderStatusAsync(Guid id, PurchaseOrderStatus status)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{id}' not found.");
        }

        order.Status = status;
        var updatedOrder = await _purchaseOrderRepository.UpdateAsync(order);
        await SaveChangesAsync();

        return _mapper.Map<PurchaseOrderDto>(updatedOrder);
    }

    /// <summary>
    /// Deletes the purchase order asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    public async Task DeletePurchaseOrderAsync(Guid id)
    {
        var order = await _purchaseOrderRepository.GetByIdAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{id}' not found.");
        }

        await _purchaseOrderRepository.DeleteAsync(order);
        await SaveChangesAsync();
    }

    /// <summary>Returns a paginated, filtered and sorted collection of purchase orders.</summary>
    /// <param name="spec">The specification that defines filtering, sorting, and paging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result containing matching purchase order DTOs.</returns>
    public async Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec, CancellationToken cancellationToken = default)
    {
        var result = await _purchaseOrderRepository.QueryAsync(spec, cancellationToken);
        var dtos = result.Items.Select(po => _mapper.Map<PurchaseOrderDto>(po)).ToList();
        return new PaginatedResult<PurchaseOrderDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>
    /// Approves the purchase order asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<PurchaseOrderDto> ApprovePurchaseOrderAsync(ApprovePurchaseOrderDto dto)
    {
        _logger.LogInformation("Approving purchase order: PurchaseOrderId={PurchaseOrderId}", dto.PurchaseOrderId);

        var order = await _purchaseOrderRepository.GetWithLinesAsync(dto.PurchaseOrderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{dto.PurchaseOrderId}' not found");
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Purchase order cannot be approved. Current status: {order.Status}");
        }

        // Update status to Approved
        order.Status = PurchaseOrderStatus.Approved;
        await _purchaseOrderRepository.UpdateAsync(order);
        await SaveChangesAsync();

        // Publish PurchaseOrderApprovedEvent
        var purchaseOrderApprovedEvent = new PurchaseOrderApprovedEvent(
            order.Id,
            order.SupplierId,
            DateTime.UtcNow
        );

        try
        {
            await EventPublisher.PublishAsync(MessagingConstants.Topics.PurchasingOrderApproved, purchaseOrderApprovedEvent);
            _logger.LogInformation("Published PurchaseOrderApprovedEvent for PO {PurchaseOrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PurchaseOrderApprovedEvent for PO {PurchaseOrderId}", order.Id);
        }

        _logger.LogInformation("Purchase order approved successfully: PurchaseOrderId={PurchaseOrderId}", order.Id);
        return _mapper.Map<PurchaseOrderDto>(order);
    }

    /// <summary>
    /// Receives the purchase order asynchronously.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(
        ReceivePurchaseOrderDto dto,
        CancellationToken cancellationToken = default) =>
        _receivePurchaseOrder.ExecuteAsync(dto, cancellationToken);

    /// <inheritdoc />
    public Task<PurchaseOrderMatchResultDto> MatchPurchaseOrderAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default) =>
        _matchPurchaseOrder.ExecuteAsync(purchaseOrderId, cancellationToken);
}
