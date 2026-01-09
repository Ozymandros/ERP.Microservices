using Dapr.Client;
using MyApp.Inventory.Application.Contracts.Services;
using MyApp.Shared.Domain.Events;

namespace MyApp.Inventory.API.BackgroundServices;

/// <summary>
/// Background service that checks for low stock and publishes alerts
/// Runs every hour
/// </summary>
public class LowStockAlertService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LowStockAlertService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public LowStockAlertService(
        IServiceProvider serviceProvider,
        ILogger<LowStockAlertService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Low Stock Alert Service starting");

        // Wait 1 minute before first check to allow services to start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckLowStockAndPublishAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking low stock");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when service is stopping
                break;
            }
        }

        _logger.LogInformation("Low Stock Alert Service stopping");
    }

    private async Task CheckLowStockAndPublishAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var warehouseStockService = scope.ServiceProvider.GetRequiredService<IWarehouseStockService>();
        var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

        _logger.LogInformation("Checking for low stock items");

        var lowStocks = await warehouseStockService.GetLowStockAsync();

        if (lowStocks.Any())
        {
            _logger.LogWarning("Found {Count} low stock items", lowStocks.Count);

            foreach (var stock in lowStocks)
            {
                _logger.LogWarning(
                    "Low stock alert: ProductId={ProductId}, WarehouseId={WarehouseId}, Available={Available}",
                    stock.ProductId, stock.WarehouseId, stock.AvailableQuantity);

                // Publish LowStockAlertEvent via Dapr
                // Note: ReorderLevel would need to be fetched from Product if needed for alerting logic
                var lowStockEvent = new LowStockAlertEvent(
                    stock.ProductId,
                    stock.WarehouseId,
                    stock.AvailableQuantity,
                    10 // Default reorder level - could be fetched from Product repository if needed
                );
                
                try
                {
                    await daprClient.PublishEventAsync(
                        "pubsub",
                        "inventory.stock.low-stock-alert",
                        lowStockEvent,
                        cancellationToken);
                    
                    _logger.LogInformation("Published LowStockAlertEvent for ProductId={ProductId}", stock.ProductId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish LowStockAlertEvent for ProductId={ProductId}", stock.ProductId);
                }
            }
        }
        else
        {
            _logger.LogInformation("No low stock items found");
        }
    }
}
