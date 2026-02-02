using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Constants;

namespace MyApp.Orders.API.BackgroundServices;

/// <summary>
/// Background service that checks for expired stock reservations and releases them
/// Runs every 5 minutes
/// </summary>
public class ReservationExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpiryService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public ReservationExpiryService(
        IServiceProvider serviceProvider,
        ILogger<ReservationExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Expiry Service starting");

        // Wait 30 seconds before first check to allow services to start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndReleaseExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired reservations");
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

        _logger.LogInformation("Reservation Expiry Service stopping");
    }

    private async Task CheckAndReleaseExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservedStockRepository = scope.ServiceProvider.GetRequiredService<IReservedStockRepository>();
        var serviceInvoker = scope.ServiceProvider.GetRequiredService<IServiceInvoker>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        _logger.LogInformation("Checking for expired reservations");

        var expiredReservations = await reservedStockRepository.GetExpiredReservationsAsync();

        if (expiredReservations.Any())
        {
            _logger.LogWarning("Found {Count} expired reservations", expiredReservations.Count);

            foreach (var reservation in expiredReservations)
            {
                try
                {
                    _logger.LogWarning(
                        "Releasing expired reservation: ReservationId={ReservationId}, ProductId={ProductId}, WarehouseId={WarehouseId}, Quantity={Quantity}, ExpiredAt={ExpiredAt}",
                        reservation.Id, reservation.ProductId, reservation.WarehouseId, reservation.Quantity, reservation.ReservedUntil);

                    // Mark reservation as expired
                    reservation.Status = ReservationStatus.Expired;
                    await reservedStockRepository.UpdateAsync(reservation);

                    // Call Inventory service to release the reserved stock via Dapr service invocation
                    try
                    {
                        await serviceInvoker.InvokeAsync(
                            ServiceNames.Inventory,
                            $"{ApiEndpoints.Inventory.Reservations}/{reservation.Id}",
                            HttpMethod.Delete,
                            cancellationToken);

                        _logger.LogInformation("Called Inventory service to release reservation {ReservationId}", reservation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to call Inventory service for reservation {ReservationId}", reservation.Id);
                    }

                    // Publish StockReleasedEvent via Dapr
                    var stockReleasedEvent = new StockReleasedEvent(
                        reservation.Id,
                        reservation.ProductId,
                        reservation.WarehouseId,
                        reservation.Quantity
                    );

                    try
                    {
                        await eventPublisher.PublishAsync(
                            MessagingConstants.Topics.InventoryStockReleased,
                            stockReleasedEvent,
                            cancellationToken);

                        _logger.LogInformation("Published StockReleasedEvent for reservation {ReservationId}", reservation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish StockReleasedEvent for reservation {ReservationId}", reservation.Id);
                    }

                    _logger.LogInformation("Released expired reservation: ReservationId={ReservationId}", reservation.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to release reservation: ReservationId={ReservationId}", reservation.Id);
                }
            }
        }
        else
        {
            _logger.LogDebug("No expired reservations found");
        }
    }
}
