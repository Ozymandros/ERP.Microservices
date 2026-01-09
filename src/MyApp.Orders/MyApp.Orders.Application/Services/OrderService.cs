using AutoMapper;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.BusinessRules;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Orders.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IOrderLineRepository _lines;
        private readonly IReservedStockRepository _reservedStockRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly DaprClient _daprClient;
        private const string PubSubName = "pubsub";

        public OrderService(
            IOrderRepository orders,
            IOrderLineRepository lines,
            IReservedStockRepository reservedStockRepository,
            IMapper mapper,
            ILogger<OrderService> logger,
            DaprClient daprClient)
        {
            _orders = orders;
            _lines = lines;
            _reservedStockRepository = reservedStockRepository;
            _mapper = mapper;
            _logger = logger;
            _daprClient = daprClient;
        }

        public async Task<OrderDto> CreateAsync(CreateUpdateOrderDto dto)
        {
            var entity = _mapper.Map<Order>(dto);
            entity.Id = Guid.NewGuid();
            entity.OrderDate = dto.OrderDate;
            entity.Status = OrderStatus.Draft;
            entity.TotalAmount = entity.Lines.Sum(l => l.LineTotal = l.Quantity * l.UnitPrice);

            foreach (var line in entity.Lines)
            {
                line.Id = Guid.NewGuid();
                line.OrderId = entity.Id;
            }

            await _orders.AddAsync(entity);

            return _mapper.Map<OrderDto>(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _orders.DeleteAsync(id);
        }

        public async Task<OrderDto> GetByIdAsync(Guid id)
        {
            var entity = await _orders.GetByIdAsync(id);
            return _mapper.Map<OrderDto>(entity!);
        }

        public async Task<IEnumerable<OrderDto>> ListAsync()
        {
            var list = await _orders.ListAsync();
            return list.Select(o => _mapper.Map<OrderDto>(o));
        }

        public async Task UpdateAsync(Guid id, CreateUpdateOrderDto dto)
        {
            var existing = await _orders.GetByIdAsync(id);
            if (existing == null) return;

            existing.OrderNumber = dto.OrderNumber;
            existing.CustomerId = dto.CustomerId;
            existing.OrderDate = dto.OrderDate;

            // Simple handling: replace lines
            existing.Lines.Clear();
            foreach (var l in dto.Lines)
            {
                var line = new OrderLine(Guid.NewGuid())
                {
                    OrderId = existing.Id,
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.Quantity * l.UnitPrice
                };
                existing.Lines.Add(line);
            }

            existing.TotalAmount = existing.Lines.Sum(x => x.LineTotal);

            await _orders.UpdateAsync(existing);
        }

        public async Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto dto)
        {
            _logger.LogInformation(
                "Creating order with reservation: OrderNumber={OrderNumber}, CustomerId={CustomerId}, WarehouseId={WarehouseId}",
                dto.OrderNumber, dto.CustomerId, dto.WarehouseId);

            // Validate order lines
            if (dto.Lines.Count == 0)
            {
                throw new InvalidOperationException("Order must have at least one line");
            }

            // Create order entity
            var order = new Order(Guid.NewGuid())
            {
                OrderNumber = dto.OrderNumber,
                CustomerId = dto.CustomerId,
                OrderDate = dto.OrderDate,
                Status = OrderStatus.Draft,
                WarehouseId = dto.WarehouseId,
                ShippingAddress = dto.ShippingAddress
            };

            // Create order lines
            foreach (var lineDto in dto.Lines)
            {
                var line = new OrderLine(Guid.NewGuid())
                {
                    OrderId = order.Id,
                    ProductId = lineDto.ProductId,
                    Quantity = lineDto.Quantity,
                    UnitPrice = lineDto.UnitPrice,
                    LineTotal = lineDto.Quantity * lineDto.UnitPrice
                };
                order.Lines.Add(line);
            }

            order.TotalAmount = order.Lines.Sum(l => l.LineTotal);

            // Validate order using business rules
            OrderInvariants.ValidateOrder(order.Lines.Count, order.TotalAmount, order.Lines.Sum(l => l.LineTotal));

            // Save order
            await _orders.AddAsync(order);

            // Reserve stock for each line via Dapr service invocation
            foreach (var line in order.Lines)
            {
                try
                {
                    _logger.LogInformation(
                        "Reserving stock for OrderLine: ProductId={ProductId}, Quantity={Quantity}, WarehouseId={WarehouseId}",
                        line.ProductId, line.Quantity, dto.WarehouseId);

                    // Call Inventory service to reserve stock
                    var reserveRequest = new
                    {
                        productId = line.ProductId,
                        warehouseId = dto.WarehouseId,
                        quantity = line.Quantity,
                        orderId = order.Id,
                        orderLineId = line.Id,
                        expiresAt = (DateTime?)null // Use default 24-hour expiry
                    };

                    var reservation = await _daprClient.InvokeMethodAsync<object, dynamic>(
                        HttpMethod.Post,
                        "inventory",
                        "api/stockoperations/reserve",
                        reserveRequest);

                    // Create ReservedStock record
                    var reservedStock = new ReservedStock(Guid.Parse(reservation.id.ToString()))
                    {
                        ProductId = line.ProductId,
                        WarehouseId = dto.WarehouseId,
                        OrderId = order.Id,
                        OrderLineId = line.Id,
                        Quantity = line.Quantity,
                        ReservedUntil = DateTime.Parse(reservation.reservedUntil.ToString()),
                        Status = ReservationStatus.Reserved
                    };

                    await _reservedStockRepository.AddAsync(reservedStock);

                    line.ReservedStockId = reservedStock.Id;
                    line.ReservedQuantity = line.Quantity;

                    _logger.LogInformation("Stock reserved successfully for OrderLine {OrderLineId}", line.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reserve stock for OrderLine {OrderLineId}, Product {ProductId}", 
                        line.Id, line.ProductId);
                    
                    // Roll back: cancel order and release any reservations made so far
                    await CancelOrderAsync(new CancelOrderDto
                    {
                        OrderId = order.Id,
                        Reason = $"Stock reservation failed: {ex.Message}"
                    });
                    
                    throw new OrderFulfillmentException(order.Id, $"Failed to reserve stock: {ex.Message}");
                }
            }

            // Update order status to Confirmed (reservations confirmed)
            order.Status = OrderStatus.Confirmed;
            await _orders.UpdateAsync(order);

            // Publish OrderCreatedEvent
            var orderCreatedEvent = new OrderCreatedEvent(
                order.Id,
                order.CustomerId,
                order.OrderNumber,
                order.Lines.Select(l => new OrderLineEvent(l.ProductId, l.Quantity, l.UnitPrice)).ToList()
            );

            try
            {
                await _daprClient.PublishEventAsync(PubSubName, "orders.order.created", orderCreatedEvent);
                _logger.LogInformation("Published OrderCreatedEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreatedEvent for Order {OrderId}", order.Id);
            }

            _logger.LogInformation("Order created successfully with reservations: OrderId={OrderId}", order.Id);
            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> FulfillOrderAsync(FulfillOrderDto dto)
        {
            _logger.LogInformation("Fulfilling order: OrderId={OrderId}", dto.OrderId);

            var order = await _orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {dto.OrderId} not found");
            }

            if (order.Status != OrderStatus.Confirmed)
            {
                throw new OrderFulfillmentException(dto.OrderId, $"Order cannot be fulfilled. Current status: {order.Status}");
            }

            // Get reservations
            var reservations = await _reservedStockRepository.GetByOrderIdAsync(dto.OrderId);
            if (reservations.Count == 0)
            {
                throw new OrderFulfillmentException(dto.OrderId, "No stock reservations found for this order");
            }

            // Mark all reservations as fulfilled
            foreach (var reservation in reservations)
            {
                if (reservation.Status != ReservationStatus.Reserved)
                {
                    throw new OrderFulfillmentException(dto.OrderId, 
                        $"Reservation {reservation.Id} is not in Reserved status: {reservation.Status}");
                }

                reservation.Status = ReservationStatus.Fulfilled;
                await _reservedStockRepository.UpdateAsync(reservation);
            }

            // Update order
            order.Status = OrderStatus.Shipped;
            order.FulfilledDate = DateTime.UtcNow;
            order.WarehouseId = dto.WarehouseId;
            order.ShippingAddress = dto.ShippingAddress;
            order.TrackingNumber = dto.TrackingNumber;

            // Mark all lines as fulfilled
            foreach (var line in order.Lines)
            {
                line.IsFulfilled = true;
            }

            await _orders.UpdateAsync(order);

            // Publish OrderFulfilledEvent
            var orderFulfilledEvent = new OrderFulfilledEvent(
                order.Id,
                dto.WarehouseId,
                order.FulfilledDate.Value,
                dto.TrackingNumber
            );

            try
            {
                await _daprClient.PublishEventAsync(PubSubName, "orders.order.fulfilled", orderFulfilledEvent);
                _logger.LogInformation("Published OrderFulfilledEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderFulfilledEvent for Order {OrderId}", order.Id);
            }

            _logger.LogInformation("Order fulfilled successfully: OrderId={OrderId}", order.Id);
            return _mapper.Map<OrderDto>(order);
        }

        public async Task CancelOrderAsync(CancelOrderDto dto)
        {
            _logger.LogInformation("Cancelling order: OrderId={OrderId}, Reason={Reason}", dto.OrderId, dto.Reason);

            var order = await _orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {dto.OrderId} not found");
            }

            if (order.Status == OrderStatus.Shipped)
            {
                throw new InvalidOperationException($"Cannot cancel shipped order {dto.OrderId}");
            }

            // Get and release all reservations
            var reservations = await _reservedStockRepository.GetByOrderIdAsync(dto.OrderId);
            foreach (var reservation in reservations)
            {
                if (reservation.Status == ReservationStatus.Reserved)
                {
                    // Call Inventory service to release reservation
                    try
                    {
                        await _daprClient.InvokeMethodAsync(
                            HttpMethod.Delete,
                            "inventory",
                            $"api/stockoperations/reservations/{reservation.Id}");

                        reservation.Status = ReservationStatus.Cancelled;
                        await _reservedStockRepository.UpdateAsync(reservation);

                        _logger.LogInformation("Released reservation {ReservationId} for Order {OrderId}", 
                            reservation.Id, dto.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to release reservation {ReservationId}", reservation.Id);
                    }
                }
            }

            // Update order status
            order.Status = OrderStatus.Cancelled;
            await _orders.UpdateAsync(order);

            // Publish OrderCancelledEvent
            var orderCancelledEvent = new OrderCancelledEvent(
                order.Id,
                dto.Reason
            );

            try
            {
                await _daprClient.PublishEventAsync(PubSubName, "orders.order.cancelled", orderCancelledEvent);
                _logger.LogInformation("Published OrderCancelledEvent for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCancelledEvent for Order {OrderId}", order.Id);
            }

            _logger.LogInformation("Order cancelled successfully: OrderId={OrderId}", order.Id);
        }
    }
}
