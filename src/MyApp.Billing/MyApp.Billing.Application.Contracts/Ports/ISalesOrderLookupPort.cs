using MyApp.Billing.Application.Contracts.DTOs;

namespace MyApp.Billing.Application.Contracts.Ports;

/// <summary>Driven port: read commercial sales order data from the Sales service.</summary>
public interface ISalesOrderLookupPort
{
    /// <summary>Loads a sales order by id, or <see langword="null"/> when not found.</summary>
    Task<SalesOrderSnapshot?> GetByIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
}