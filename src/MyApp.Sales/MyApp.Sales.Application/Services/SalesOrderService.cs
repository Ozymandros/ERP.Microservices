using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Sales.Application.UseCases.SalesOrders;
using MyApp.Sales.Domain;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Sales.Application.Services;

/// <summary>Implements sales order management and commercial workflow operations, including CRUD, quote creation, and quote confirmation via the Orders service.</summary>
public class SalesOrderService : AppServiceBase, ISalesOrderService
{
    private readonly ISalesOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SalesOrderService> _logger;
    private readonly CheckStockAvailabilityUseCase _checkStock;
    private readonly ConfirmQuoteUseCase _confirmQuote;
    private readonly CreateReturnUseCase _createReturn;

  public SalesOrderService(
        ISalesOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IMapper mapper,
        ILogger<SalesOrderService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        CheckStockAvailabilityUseCase checkStock,
        ConfirmQuoteUseCase confirmQuote,
        CreateReturnUseCase createReturn)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Sales)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _mapper = mapper;
        _logger = logger;
        _checkStock = checkStock;
        _confirmQuote = confirmQuote;
        _createReturn = createReturn;
    }

    /// <summary>Returns the sales order with the given identifier, or <see langword="null"/> if not found.</summary>
    /// Gets the sales order by id asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="SalesOrderDto"/>, or <see langword="null"/>.</returns>
    public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        return order != null ? _mapper.Map<SalesOrderDto>(order) : null;
    }

    /// <summary>Returns the sales order with the given order number, or <see langword="null"/> if not found.</summary>
    /// Gets the sales order by order number asynchronously.
    /// <param name="orderNumber">The order Number.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="SalesOrderDto"/>, or <see langword="null"/>.</returns>
    public async Task<SalesOrderDto?> GetSalesOrderByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
        return order != null ? _mapper.Map<SalesOrderDto>(order) : null;
    }

    /// <summary>Returns all sales orders as an enumerable sequence.</summary>
    /// Lists sales orders asynchronously.
    public async Task<IEnumerable<SalesOrderDto>> ListSalesOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.ListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<SalesOrderDto>>(orders);
    }

    /// <summary>Returns a paginated list of sales orders.</summary>
    /// Lists sales orders paginated asynchronously.
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="PaginatedResult{T}"/> containing the requested page of sales orders.</returns>
    public async Task<PaginatedResult<SalesOrderDto>> ListSalesOrdersPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var paginatedOrders = await _orderRepository.GetAllPaginatedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);
        var orderDtos = _mapper.Map<IEnumerable<SalesOrderDto>>(paginatedOrders.Items);
        return new PaginatedResult<SalesOrderDto>(orderDtos, paginatedOrders.PageNumber, paginatedOrders.PageSize, paginatedOrders.TotalCount);
    }

    /// <summary>Creates a new sales order, validates customer existence, computes line totals, and persists the order.</summary>
    /// Creates a sales order asynchronously.
    /// <param name="dto">The dto.</param>
    /// <returns>The created <see cref="SalesOrderDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the customer specified by <paramref name="dto"/> does not exist.</exception>
    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateUpdateSalesOrderDto dto)
    {
        // Validate customer exists
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID '{dto.CustomerId}' not found.");
        }

        var order = _mapper.Map<SalesOrder>(dto);
        order.Id = Guid.NewGuid();
        order.OrderNumber = await GenerateOrderNumberAsync();
        order.OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate;

        // Calculate total from lines if provided
        if (dto.Lines.Count() != 0)
        {
            var lines = new List<SalesOrderLine>();
            foreach (var lineDto in dto.Lines)
            {
                var line = _mapper.Map<SalesOrderLine>(lineDto);
                line.Id = Guid.NewGuid();
                line.SalesOrderId = order.Id;
                line.LineTotal = line.Quantity * line.UnitPrice;
                lines.Add(line);
            }
            order.Lines = lines;
            order.TotalAmount = lines.Sum(l => l.LineTotal);
        }

        var createdOrder = await _orderRepository.AddAsync(order);
        await SaveChangesAsync();
        return _mapper.Map<SalesOrderDto>(createdOrder);
    }

    /// <summary>Generates a unique order number using timestamp, sequential count, and a random suffix.</summary>
    /// <returns>A unique order number string in the format <c>SO-{timestamp}-{count}-{random}</c>.</returns>
    private async Task<string> GenerateOrderNumberAsync()
    {
        // Example: Use a timestamp and a random suffix for uniqueness (replace with a DB sequence or other logic as needed)
        var now = DateTime.UtcNow;
        var random = Guid.NewGuid().ToString()[..8];
        var count = (await _orderRepository.GetAllAsync()).Count() + 1; // Not perfect for concurrency, but placeholder
        return $"SO-{now:yyyyMMddHHmmss}-{count}-{random}";
    }

    /// <summary>Updates the sales order with the given identifier, recalculates line totals if lines are provided, and persists the changes.</summary>
    /// Updates the sales order asynchronously.
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <returns>The updated <see cref="SalesOrderDto"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the order or the referenced customer does not exist.</exception>
    public async Task<SalesOrderDto> UpdateSalesOrderAsync(Guid id, CreateUpdateSalesOrderDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {id} not found.");

        // Validate customer exists if changed
        if (order.CustomerId != dto.CustomerId)
        {
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
            if (customer == null)
                throw new InvalidOperationException($"Customer with ID {dto.CustomerId} not found.");
        }

        // Update basic properties
        // Remove assignment from DTO, generate server-side
        // order.OrderNumber = dto.OrderNumber;
        order.CustomerId = dto.CustomerId;
        order.Status = (SalesOrderStatus)dto.Status;

        // Recalculate lines and total if provided
        if (dto.Lines.Count() != 0)
        {
            order.Lines.Clear();
            foreach (var lineDto in dto.Lines)
            {
                var line = _mapper.Map<SalesOrderLine>(lineDto);
                line.Id = Guid.NewGuid();
                line.SalesOrderId = order.Id;
                line.LineTotal = line.Quantity * line.UnitPrice;
                order.Lines.Add(line);
            }
            order.TotalAmount = order.Lines.Sum(l => l.LineTotal);
        }

        await _orderRepository.UpdateAsync(order);
        await SaveChangesAsync();
        return _mapper.Map<SalesOrderDto>(order);
    }

    /// <summary>Deletes the sales order with the given identifier.</summary>
    /// Deletes the sales order asynchronously.
    /// <param name="id">The id.</param>
    public async Task DeleteSalesOrderAsync(Guid id)
    {
        await _orderRepository.DeleteAsync(id);
        await SaveChangesAsync();
    }

    /// <summary>Returns a paginated list of sales orders that satisfy the given specification.</summary>
    /// Query sales orders asynchronously.
    /// <param name="spec">The spec.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="PaginatedResult{T}"/> containing matching sales orders.</returns>
    public async Task<PaginatedResult<SalesOrderDto>> QuerySalesOrdersAsync(ISpecification<SalesOrder> spec, CancellationToken cancellationToken = default)
    {
        var result = await _orderRepository.QueryAsync(spec, cancellationToken);
        var dtos = result.Items.Select(so => _mapper.Map<SalesOrderDto>(so)).ToList();
        return new PaginatedResult<SalesOrderDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
    }

    /// <summary>Creates a quote with stock availability validation and publishes a <c>SalesOrderCreatedEvent</c>.</summary>
    /// Creates a quote asynchronously.
    /// <param name="dto">The dto.</param>
    /// <returns>The created quote as a <see cref="SalesOrderDto"/> with <c>IsQuote</c> set to <see langword="true"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the customer specified by <paramref name="dto"/> does not exist.</exception>
    public async Task<SalesOrderDto> CreateQuoteAsync(CreateQuoteDto dto)
    {
        _logger.LogInformation(
            "Creating quote: OrderNumber={OrderNumber}, CustomerId={CustomerId}",
            new MyApp.Shared.Domain.Security.LogSanitizer().Sanitize(dto.OrderNumber), dto.CustomerId);

        // Validate customer exists
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {dto.CustomerId} not found");

        // Check stock availability for all lines
        var stockChecks = await _checkStock.ExecuteAsync(dto.Lines);
        var unavailableItems = stockChecks.Where(s => !s.IsAvailable).ToList();

        if (unavailableItems.Count() != 0)
        {
            _logger.LogWarning(
                "Quote creation - some items have insufficient stock: {UnavailableCount} items",
                unavailableItems.Count);
            // Continue with quote creation but log the warning
        }

        // Create sales order as quote
        var quote = new SalesOrder(Guid.NewGuid())
        {
            OrderNumber = dto.OrderNumber,
            CustomerId = dto.CustomerId,
            OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
            Status = SalesOrderStatus.Draft,
            IsQuote = true,
            QuoteExpiryDate = DateTime.UtcNow.AddDays(dto.ValidityDays)
        };

        // Create order lines
        foreach (var lineDto in dto.Lines)
        {
            var line = new SalesOrderLine(Guid.NewGuid())
            {
                SalesOrderId = quote.Id,
                ProductId = lineDto.ProductId,
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                LineTotal = lineDto.Quantity * lineDto.UnitPrice
            };
            quote.Lines.Add(line);
        }

        quote.TotalAmount = quote.Lines.Sum(l => l.LineTotal);

        await _orderRepository.AddAsync(quote);
        await SaveChangesAsync();

        // Publish SalesOrderCreatedEvent
        var salesOrderCreatedEvent = new SalesOrderCreatedEvent(
            quote.Id,
            quote.CustomerId,
            quote.OrderNumber,
            quote.IsQuote,
            quote.TotalAmount
        );

        try
        {
            await EventPublisher.PublishAsync(MessagingConstants.Topics.SalesOrderCreated, salesOrderCreatedEvent);
            _logger.LogInformation("Published SalesOrderCreatedEvent for Quote {QuoteId}", quote.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SalesOrderCreatedEvent for Quote {QuoteId}", quote.Id);
        }

        _logger.LogInformation(
            "Quote created successfully: QuoteId={QuoteId}, ExpiresAt={ExpiresAt}",
            quote.Id, quote.QuoteExpiryDate);

        return _mapper.Map<SalesOrderDto>(quote);
    }

    /// <summary>Confirms a quote by re-validating stock, creating a fulfillment order via the Orders service, and publishing a <c>SalesOrderConfirmedEvent</c>.</summary>
    /// Confirm quote asynchronously.
    /// <param name="dto">The dto.</param>
    /// <returns>The confirmed <see cref="SalesOrderDto"/> linked to the created fulfillment order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the quote does not exist, is not in Draft status, has expired, has insufficient stock, or the fulfillment order creation fails.</exception>
    public Task<SalesOrderDto> ConfirmQuoteAsync(ConfirmQuoteDto dto) =>
        _confirmQuote.ExecuteAsync(dto);

    public Task<List<StockAvailabilityCheckDto>> CheckStockAvailabilityAsync(List<CreateUpdateSalesOrderLineDto> lines) =>
        _checkStock.ExecuteAsync(lines);

    /// <inheritdoc />
    public async Task<SalesOrderCacheInvalidationDto?> MarkShippedFromFulfillmentAsync(
        OrderFulfilledEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(@event.OrderType, OrderTypes.Outbound, StringComparison.OrdinalIgnoreCase)
            || !@event.ExternalOrderId.HasValue)
        {
            return null;
        }

        var salesOrder = await _orderRepository.GetByIdAsync(@event.ExternalOrderId.Value, cancellationToken);
        if (salesOrder is null)
        {
            _logger.LogWarning("Sales order {SalesOrderId} not found for fulfillment event", @event.ExternalOrderId);
            return null;
        }

        if (salesOrder.Status is SalesOrderStatus.Shipped or SalesOrderStatus.Invoiced or SalesOrderStatus.Cancelled)
        {
            return new SalesOrderCacheInvalidationDto(salesOrder.Id, salesOrder.OrderNumber);
        }

        if (salesOrder.Status != SalesOrderStatus.Confirmed)
        {
            _logger.LogWarning(
                "Skipping ship status for sales order {SalesOrderId}; status is {Status}",
                salesOrder.Id,
                salesOrder.Status);
            return null;
        }

        salesOrder.Status = SalesOrderStatus.Shipped;
        await _orderRepository.UpdateAsync(salesOrder);
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sales order {SalesOrderId} marked Shipped from order {OrderId}", salesOrder.Id, @event.OrderId);
        return new SalesOrderCacheInvalidationDto(salesOrder.Id, salesOrder.OrderNumber);
    }

    /// <inheritdoc />
    public async Task<SalesOrderCacheInvalidationDto?> MarkCancelledFromOrderAsync(
        OrderCancelledEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (!@event.ExternalOrderId.HasValue)
            return null;

        var salesOrder = await _orderRepository.GetByIdAsync(@event.ExternalOrderId.Value, cancellationToken);
        if (salesOrder is null)
            return null;

        if (salesOrder.Status is SalesOrderStatus.Cancelled or SalesOrderStatus.Invoiced)
            return new SalesOrderCacheInvalidationDto(salesOrder.Id, salesOrder.OrderNumber);

        if (salesOrder.Status is not (SalesOrderStatus.Confirmed or SalesOrderStatus.Shipped))
            return null;

        salesOrder.Status = SalesOrderStatus.Cancelled;
        await _orderRepository.UpdateAsync(salesOrder);
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sales order {SalesOrderId} cancelled from operational order {OrderId}", salesOrder.Id, @event.OrderId);
        return new SalesOrderCacheInvalidationDto(salesOrder.Id, salesOrder.OrderNumber);
    }

    /// <inheritdoc />
    public async Task<SalesOrderCacheInvalidationDto?> MarkInvoicedFromBillingAsync(
        InvoiceIssuedEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (!@event.OrderId.HasValue)
            return null;

        var salesOrder = await _orderRepository.GetByIdAsync(@event.OrderId.Value, cancellationToken);
        if (salesOrder is null)
            return null;

        if (salesOrder.Status == SalesOrderStatus.Invoiced)
            return new SalesOrderCacheInvalidationDto(salesOrder.Id, salesOrder.OrderNumber);

        if (salesOrder.Status is not (SalesOrderStatus.Confirmed or SalesOrderStatus.Shipped))
        {
            _logger.LogWarning(
                "Cannot mark sales order {SalesOrderId} Invoiced from status {Status}",
                salesOrder.Id,
                salesOrder.Status);
            return null;
        }

        salesOrder.Status = SalesOrderStatus.Invoiced;
        await _orderRepository.UpdateAsync(salesOrder);
        await SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sales order {SalesOrderId} marked Invoiced", salesOrder.Id);
        return new SalesOrderCacheInvalidationDto(salesOrder.Id, salesOrder.OrderNumber);
    }

    /// <inheritdoc />
    public Task<SalesOrderDto> CreateReturnAsync(
        Guid salesOrderId,
        CreateSalesReturnDto dto,
        CancellationToken cancellationToken = default) =>
        _createReturn.ExecuteAsync(salesOrderId, dto, cancellationToken);
}
