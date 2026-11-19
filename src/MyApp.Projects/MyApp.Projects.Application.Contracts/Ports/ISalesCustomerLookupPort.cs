namespace MyApp.Projects.Application.Contracts.Ports;

public interface ISalesCustomerLookupPort
{
    Task<SalesCustomerSnapshot?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}