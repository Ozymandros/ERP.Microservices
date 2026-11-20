using Microsoft.Extensions.Logging;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Sales.Application.Contracts.Ports;

namespace MyApp.Sales.Application.UseCases.SalesOrders;

public sealed class CheckStockAvailabilityUseCase(
    IInventoryAvailabilityPort inventoryAvailability,
    ILogger<CheckStockAvailabilityUseCase> logger)
{
    public async Task<List<StockAvailabilityCheckDto>> ExecuteAsync(
        List<CreateUpdateSalesOrderLineDto> lines,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking stock availability for {LineCount} items", lines.Count);
        var results = new List<StockAvailabilityCheckDto>();

        foreach (var line in lines)
        {
            try
            {
                var availability = await inventoryAvailability.GetByProductIdAsync(line.ProductId, cancellationToken);
                if (availability is null)
                {
                    results.Add(Unavailable(line));
                    continue;
                }

                results.Add(new StockAvailabilityCheckDto
                {
                    ProductId = line.ProductId,
                    RequestedQuantity = line.Quantity,
                    AvailableQuantity = availability.TotalAvailable,
                    IsAvailable = availability.TotalAvailable >= line.Quantity,
                    WarehouseStock = availability.WarehouseStocks
                        .Select(ws => new WarehouseAvailabilityDto
                        {
                            WarehouseId = ws.WarehouseId,
                            WarehouseName = ws.WarehouseName,
                            AvailableQuantity = ws.AvailableQuantity
                        }).ToList()
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check stock for Product {ProductId}", line.ProductId);
                results.Add(Unavailable(line));
            }
        }

        return results;
    }

    private static StockAvailabilityCheckDto Unavailable(CreateUpdateSalesOrderLineDto line) =>
        new()
        {
            ProductId = line.ProductId,
            RequestedQuantity = line.Quantity,
            AvailableQuantity = 0,
            IsAvailable = false,
            WarehouseStock = []
        };
}