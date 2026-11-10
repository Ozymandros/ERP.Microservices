using System.Net;
using Microsoft.Extensions.Logging;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Sales.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Projects.Application.Integration;

/// <summary>Reads customers from the Sales service (Dapr service invocation).</summary>
public interface ISalesCustomerClient
{
    /// <summary>Gets a customer, or null when it does not exist.</summary>
    Task<CustomerDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}

/// <summary>Reads products from the Inventory service (Dapr service invocation).</summary>
public interface IInventoryProductClient
{
    /// <summary>Gets a product, or null when it does not exist.</summary>
    Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}

/// <summary>Creates operational orders in the Orders service (Dapr service invocation).</summary>
public interface IOrdersClient
{
    /// <summary>Creates an order with stock reservation.</summary>
    Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto request, CancellationToken cancellationToken = default);
}

/// <summary>Helpers shared by the Dapr clients.</summary>
internal static class ServiceInvocationErrors
{
    /// <summary>Returns true when the exception chain contains an HTTP 404 response.</summary>
    public static bool IsNotFound(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { StatusCode: HttpStatusCode.NotFound })
                return true;
        }

        return false;
    }
}

/// <summary><see cref="ISalesCustomerClient"/> over <see cref="IServiceInvoker"/>.</summary>
public class SalesCustomerClient(IServiceInvoker serviceInvoker, ILogger<SalesCustomerClient> logger) : ISalesCustomerClient
{
    /// <inheritdoc />
    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await serviceInvoker.InvokeAsync<CustomerDto>(
                ServiceNames.Sales, $"{ApiEndpoints.Sales.Customers}/{customerId}", HttpMethod.Get, cancellationToken);
        }
        catch (Exception ex) when (ServiceInvocationErrors.IsNotFound(ex))
        {
            logger.LogWarning("Customer {CustomerId} not found in Sales", customerId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Customer {customerId} could not be validated against the Sales service.", ex);
        }
    }
}

/// <summary><see cref="IInventoryProductClient"/> over <see cref="IServiceInvoker"/>.</summary>
public class InventoryProductClient(IServiceInvoker serviceInvoker, ILogger<InventoryProductClient> logger) : IInventoryProductClient
{
    /// <inheritdoc />
    public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await serviceInvoker.InvokeAsync<ProductDto>(
                ServiceNames.Inventory, $"{ApiEndpoints.Inventory.Products}/{productId}", HttpMethod.Get, cancellationToken);
        }
        catch (Exception ex) when (ServiceInvocationErrors.IsNotFound(ex))
        {
            logger.LogWarning("Product {ProductId} not found in Inventory", productId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Product {productId} could not be validated against the Inventory service.", ex);
        }
    }
}

/// <summary><see cref="IOrdersClient"/> over <see cref="IServiceInvoker"/>.</summary>
public class OrdersClient(IServiceInvoker serviceInvoker) : IOrdersClient
{
    /// <inheritdoc />
    public Task<OrderDto> CreateOrderWithReservationAsync(CreateOrderWithReservationDto request, CancellationToken cancellationToken = default) =>
        serviceInvoker.InvokeAsync<CreateOrderWithReservationDto, OrderDto>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.WithReservation,
            HttpMethod.Post,
            request,
            cancellationToken);
}
