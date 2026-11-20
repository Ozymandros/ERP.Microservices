using System.Net;
using Microsoft.Extensions.Logging;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Billing.Infrastructure.Adapters;

/// <summary>Loads sales orders via Dapr service invocation and maps to <see cref="SalesOrderSnapshot"/>.</summary>
public sealed class SalesOrderLookupAdapter(
    IServiceInvoker serviceInvoker,
    ILogger<SalesOrderLookupAdapter> logger) : ISalesOrderLookupPort
{
    /// <inheritdoc />
    public async Task<SalesOrderSnapshot?> GetByIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wire = await serviceInvoker.InvokeAsync<SalesOrderWireDto>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Base}/{salesOrderId}",
                HttpMethod.Get,
                cancellationToken);

            return Map(wire);
        }
        catch (Exception ex) when (IsNotFound(ex))
        {
            logger.LogWarning("Sales order {SalesOrderId} not found", salesOrderId);
            return null;
        }
    }

    private static SalesOrderSnapshot Map(SalesOrderWireDto wire) =>
        new(
            wire.Id,
            wire.OrderNumber ?? string.Empty,
            wire.CustomerId,
            (wire.Lines ?? []).Select(l => new SalesOrderLineSnapshot(l.ProductId, l.Quantity, l.UnitPrice)).ToList());

    private static bool IsNotFound(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { StatusCode: HttpStatusCode.NotFound })
                return true;
        }

        return false;
    }
}