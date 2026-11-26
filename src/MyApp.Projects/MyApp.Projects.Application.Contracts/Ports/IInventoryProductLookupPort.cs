namespace MyApp.Projects.Application.Contracts.Ports;

public interface IInventoryProductLookupPort
{
    Task<InventoryProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}