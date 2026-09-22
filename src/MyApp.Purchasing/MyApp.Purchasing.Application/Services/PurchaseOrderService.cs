using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
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
    private readonly IServiceInvoker _serviceInvoker;

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
    /// <param name="serviceInvoker">The service Invoker.</param>
    public PurchaseOrderService(
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseOrderLineRepository lineRepository,
        ISupplierRepository supplierRepository,
        IMapper mapper,
        ILogger<PurchaseOrderService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IServiceInvoker serviceInvoker)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Purchasing)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _lineRepository = lineRepository;
        _supplierRepository = supplierRepository;
        _mapper = mapper;
        _logger = logger;        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Gets the purchase order by id asynchronously.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var order = await _purchaseOrderRepository.GetWithLinesAsync(id);
        return order == null ? null : _mapper.Map<PurchaseOrderDto>(order);
    }

    /// <summary>
    /// Gets the purchase order by order number asynchronously.
    /// </summary>
    /// <param name="orderNumber">The order Number.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public async Task<PurchaseOrderDto?> GetPurchaseOrderByOrderNumberAsync(string orderNumber)
    {
        var order = await _purchaseOrderRepository.GetByOrderNumberAsync(orderNumber);
        return order == null ? null : _mapper.Map<PurchaseOrderDto>(order);
    }

    /// <summary>
    /// Gets all purchase orders asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
    {
        var orders = await _purchaseOrderRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    /// <summary>
    /// Gets the purchase orders by supplier asynchronously.
    /// </summary>
    /// <param name="supplierId">The supplier Id.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersBySupplierAsync(Guid supplierId)
    {
        var orders = await _purchaseOrderRepository.GetBySuppliersIdAsync(supplierId);
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(orders);
    }

    /// <summary>
    /// Gets the purchase orders by status asynchronously.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status)
    {
        var orders = await _purchaseOrderRepository.GetByStatusAsync(status);
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
        if (order.Lines.Any())
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
        if (order.Lines.Any())
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
    /// <returns>A paginated result containing matching purchase order DTOs.</returns>
    public async Task<PaginatedResult<PurchaseOrderDto>> QueryPurchaseOrdersAsync(ISpecification<PurchaseOrder> spec)
    {
        var result = await _purchaseOrderRepository.QueryAsync(spec);
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
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public async Task<PurchaseOrderDto> ReceivePurchaseOrderAsync(ReceivePurchaseOrderDto dto)
    {
        _logger.LogInformation(
            "Receiving purchase order: PurchaseOrderId={PurchaseOrderId}, WarehouseId={WarehouseId}",
            dto.PurchaseOrderId, dto.WarehouseId);

        var order = await _purchaseOrderRepository.GetWithLinesAsync(dto.PurchaseOrderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Purchase order with ID '{dto.PurchaseOrderId}' not found");
        }

        if (order.Status != PurchaseOrderStatus.Approved)
        {
            throw new InvalidOperationException($"Purchase order cannot be received. Current status: {order.Status}");
        }

        // Process each received line
        foreach (var receivedLine in dto.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == receivedLine.PurchaseOrderLineId);
            if (orderLine == null)
            {
                _logger.LogWarning(
                    "Purchase order line not found: PurchaseOrderLineId={PurchaseOrderLineId}",
                    receivedLine.PurchaseOrderLineId);
                continue;
            }

            // Update line received quantity
            orderLine.ReceivedQuantity += receivedLine.ReceivedQuantity;
            orderLine.IsFullyReceived = orderLine.ReceivedQuantity >= orderLine.Quantity;

            // Call Orders service to create an Inbound Order (Operational)
            try
            {
                var createOrderRequest = new CreateUpdateOrderDto
                {
                    OrderNumber = $"{order.OrderNumber}-REC",
                    Type = OrderType.Inbound,
                    SourceId = order.SupplierId,  // Origin supplier
                    TargetId = dto.WarehouseId,   // Destination warehouse
                    ExternalOrderId = order.Id,   // Link to PurchaseOrder
                    WarehouseId = dto.WarehouseId,
                    OrderDate = dto.ReceivedDate,
                    Lines = new List<CreateOrderLineDto>
                    {
                        new CreateOrderLineDto
                        {
                            ProductId = orderLine.ProductId,
                            Quantity = receivedLine.ReceivedQuantity
                        }
                    }
                };

                var fulfillmentOrder = await _serviceInvoker.InvokeAsync<CreateUpdateOrderDto, OrderDto>(
                    ServiceNames.Orders,
                    ApiEndpoints.Orders.Base,
                    HttpMethod.Post,
                    createOrderRequest);

                _logger.LogInformation(
                    "Created Inbound Order {OrderId} for Product {ProductId} to Warehouse {WarehouseId}",
                    fulfillmentOrder.Id, orderLine.ProductId, dto.WarehouseId);

                // Immediately fulfill the order to trigger inventory adjustment
                var fulfillRequest = new FulfillOrderDto
                {
                    OrderId = fulfillmentOrder.Id,
                    WarehouseId = dto.WarehouseId,
                    ShippingAddress = null,
                    TrackingNumber = $"REC-{order.OrderNumber}"
                };

                await _serviceInvoker.InvokeAsync<FulfillOrderDto, OrderDto>(
                    ServiceNames.Orders,
                    $"{ApiEndpoints.Orders.Base}/{fulfillmentOrder.Id}/fulfill",
                    HttpMethod.Post,
                    fulfillRequest);

                _logger.LogInformation("Fulfilled Inbound Order {OrderId}", fulfillmentOrder.Id);

                // Publish PurchaseOrderLineReceivedEvent
                var lineReceivedEvent = new PurchaseOrderLineReceivedEvent(
                    order.Id,
                    orderLine.Id,
                    orderLine.ProductId,
                    receivedLine.ReceivedQuantity,
                    dto.WarehouseId
                );

                await EventPublisher.PublishAsync(MessagingConstants.Topics.PurchasingLineReceived, lineReceivedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create Inbound Order for Product {ProductId} from PO {PurchaseOrderId}",
                    orderLine.ProductId, order.Id);
                throw;
            }
        }

        // Check if all lines are fully received
        bool allLinesReceived = order.Lines.All(l => l.IsFullyReceived);

        // Update order
        order.ReceivedDate = dto.ReceivedDate;
        order.ReceivingWarehouseId = dto.WarehouseId;
        order.IsReceived = allLinesReceived;
        order.Status = allLinesReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.Approved;

        await _purchaseOrderRepository.UpdateAsync(order);
        await SaveChangesAsync();

        // Publish PurchaseOrderReceivedEvent if fully received
        if (allLinesReceived)
        {
            var purchaseOrderReceivedEvent = new PurchaseOrderReceivedEvent(
                order.Id,
                dto.WarehouseId,
                dto.ReceivedDate
            );

            try
            {
                await EventPublisher.PublishAsync("purchasing.order.received", purchaseOrderReceivedEvent);
                _logger.LogInformation("Published PurchaseOrderReceivedEvent for PO {PurchaseOrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PurchaseOrderReceivedEvent for PO {PurchaseOrderId}", order.Id);
            }
        }

        _logger.LogInformation(
            "Purchase order receiving processed: PurchaseOrderId={PurchaseOrderId}, Status={Status}",
            order.Id, order.Status);

        return _mapper.Map<PurchaseOrderDto>(order);
    }
}
