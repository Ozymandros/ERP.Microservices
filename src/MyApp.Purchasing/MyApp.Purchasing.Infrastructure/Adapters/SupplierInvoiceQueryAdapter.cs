using MyApp.Purchasing.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Purchasing.Infrastructure.Adapters;

public sealed class SupplierInvoiceQueryAdapter(IServiceInvoker serviceInvoker) : ISupplierInvoiceQueryPort
{
    public async Task<IReadOnlyList<SupplierInvoiceSnapshot>> GetByPurchaseOrderIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var wire = await serviceInvoker.InvokeAsync<List<SupplierInvoiceWire>>(
            ServiceNames.Billing,
            $"{ApiEndpoints.Billing.SupplierInvoices}/by-purchase-order/{purchaseOrderId}",
            HttpMethod.Get,
            cancellationToken);

        return wire.Select(i => new SupplierInvoiceSnapshot(
            i.Id,
            (i.Lines ?? []).Select(l => new SupplierInvoiceLineSnapshot(l.ProductId, l.Quantity)).ToList())).ToList();
    }

    internal sealed class SupplierInvoiceWire
    {
        public Guid Id { get; init; }
        public List<SupplierInvoiceLineWire>? Lines { get; init; }
    }

    internal sealed class SupplierInvoiceLineWire
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }
}