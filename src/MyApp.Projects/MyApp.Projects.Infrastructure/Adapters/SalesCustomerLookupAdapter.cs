using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Projects.Infrastructure.Adapters;

public sealed class SalesCustomerLookupAdapter(
    IServiceInvoker serviceInvoker,
    ILogger<SalesCustomerLookupAdapter> logger) : ISalesCustomerLookupPort
{
    public async Task<SalesCustomerSnapshot?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wire = await serviceInvoker.InvokeAsync<CustomerWire>(
                ServiceNames.Sales,
                $"{ApiEndpoints.Sales.Customers}/{customerId}",
                HttpMethod.Get,
                cancellationToken);
            return new SalesCustomerSnapshot(wire.Id, wire.Name ?? string.Empty);
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

    internal sealed class CustomerWire
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
    }
}