using MyApp.Crm.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Crm.Infrastructure.Adapters;

/// <summary>
/// Sales Quote Creation Adapter.
/// </summary>
/// <param name="serviceInvoker">The service invoker.</param>
public sealed class SalesQuoteCreationAdapter(IServiceInvoker serviceInvoker) : ISalesQuoteCreationPort
{
    /// <summary>
    /// Create Quote Async.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sales quote snapshot.</returns>
    public async Task<SalesQuoteSnapshot> CreateQuoteAsync(
        CreateSalesQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var wire = new CreateQuoteWire
        {
            OrderNumber = request.OrderNumber,
            CustomerId = request.CustomerId,
            OrderDate = request.OrderDate,
            ValidityDays = request.ValidityDays,
            Lines = request.Lines.Select(l => new QuoteLineWire
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        var result = await serviceInvoker.InvokeAsync<CreateQuoteWire, SalesQuoteWire>(
            ServiceNames.Sales,
            ApiEndpoints.Sales.Quotes,
            HttpMethod.Post,
            wire,
            cancellationToken);

        return new SalesQuoteSnapshot(result.Id, result.OrderNumber ?? string.Empty);
    }

    /// <summary>
    /// Sales Quote Wire.
    /// </summary>
    internal sealed class SalesQuoteWire
    {
        /// <summary>
        /// The id.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// The order number.
        /// </summary>
        public string? OrderNumber { get; init; }
    }

    /// <summary>
    /// Create Quote Wire.
    /// </summary>
    internal sealed class CreateQuoteWire
    {
        /// <summary>
        /// The order number.
        /// </summary>
        public string? OrderNumber { get; init; }

        /// <summary>
        /// The customer id.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// The order date.
        /// </summary>
        public DateTime OrderDate { get; init; }

        /// <summary>
        /// The validity days.
        /// </summary>
        public int ValidityDays { get; init; }

        /// <summary>
        /// The lines.
        /// </summary>
        public List<QuoteLineWire>? Lines { get; init; }
    }

    internal sealed class QuoteLineWire
    {
        /// <summary>
        /// The product id.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// The quantity.
        /// </summary>
        public int Quantity { get; init; }
        
        /// <summary>
        /// The unit price.
        /// </summary>
        public decimal UnitPrice { get; init; }
    }
}