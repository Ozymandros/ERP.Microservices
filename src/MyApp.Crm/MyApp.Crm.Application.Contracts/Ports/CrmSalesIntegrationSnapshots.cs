namespace MyApp.Crm.Application.Contracts.Ports;

public sealed record SalesQuoteSnapshot(Guid Id, string OrderNumber);

public sealed record CreateSalesQuoteRequest(
    string OrderNumber,
    Guid CustomerId,
    DateTime OrderDate,
    int ValidityDays,
    IReadOnlyList<CrmQuoteLineRequest> Lines);

public sealed record CrmQuoteLineRequest(Guid ProductId, int Quantity, decimal UnitPrice);