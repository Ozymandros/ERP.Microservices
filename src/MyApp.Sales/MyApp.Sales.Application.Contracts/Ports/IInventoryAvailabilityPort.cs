namespace MyApp.Sales.Application.Contracts.Ports;

/// <summary>Reads product stock availability from Inventory.</summary>
public interface IInventoryAvailabilityPort
{
    Task<ProductAvailabilitySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}