namespace MyApp.Billing.Application.Contracts.DTOs;

/// <summary>Minimal sales-order read model used by Billing (anti-corruption layer).</summary>
public sealed record SalesOrderSnapshot(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    IReadOnlyList<SalesOrderLineSnapshot> Lines);

/// <summary>Line on <see cref="SalesOrderSnapshot"/>.</summary>
public sealed record SalesOrderLineSnapshot(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);