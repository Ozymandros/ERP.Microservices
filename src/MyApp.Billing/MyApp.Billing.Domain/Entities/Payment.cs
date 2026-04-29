using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Payment entity - represents a payment made against an invoice
/// </summary>
public class Payment : AuditableEntity<Guid>
{
    public Payment(Guid id, Guid invoiceId, decimal amount, string currency, string method, DateTime paidAt, string? externalPaymentId = null)
        : base(id)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        Currency = currency;
        Method = method;
        PaidAt = paidAt;
        ExternalPaymentId = externalPaymentId;
        Status = PaymentStatus.Completed;
    }

    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public string Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime PaidAt { get; private set; }
    public string? ExternalPaymentId { get; private set; }

    // Navigation
    public Invoice? Invoice { get; private set; }
}

/// <summary>
/// Payment status enum
/// </summary>
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}
