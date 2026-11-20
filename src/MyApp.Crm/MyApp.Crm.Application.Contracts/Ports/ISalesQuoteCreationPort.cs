namespace MyApp.Crm.Application.Contracts.Ports;

public interface ISalesQuoteCreationPort
{
    Task<SalesQuoteSnapshot> CreateQuoteAsync(
        CreateSalesQuoteRequest request,
        CancellationToken cancellationToken = default);
}