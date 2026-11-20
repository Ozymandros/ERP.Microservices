namespace MyApp.Purchasing.Application.Contracts.Ports;

public interface ISupplierInvoiceQueryPort
{
    Task<IReadOnlyList<SupplierInvoiceSnapshot>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default);
}