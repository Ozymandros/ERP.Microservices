namespace MyApp.Billing.Infrastructure.Adapters;

/// <summary>JSON shape returned by the Sales API (wire format).</summary>
internal sealed class SalesOrderWireDto
{
    public Guid Id { get; init; }
    public string? OrderNumber { get; init; }
    public Guid CustomerId { get; init; }
    public List<SalesOrderLineWireDto>? Lines { get; init; }
}

internal sealed class SalesOrderLineWireDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}