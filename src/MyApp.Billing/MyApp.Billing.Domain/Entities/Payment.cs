using MyApp.Shared.Domain.Entities;

namespace MyApp.Billing.Domain.Entities;

/// <summary>
/// Payment entity - represents a payment made against an invoice
/// </summary>
public class Payment : AuditableEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the Payment class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="invoiceId">The invoice Id.</param>
    /// <param name="amount">The amount.</param>
    /// <param name="currency">The currency.</param>
    /// <param name="method">The method.</param>
    /// <param name="paidAt">The paid At.</param>
    /// <param name="externalPaymentId">The external Payment Id.</param>
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

    /// <summary>Gets the identifier of the invoice this payment is applied to.</summary>
    public Guid InvoiceId { get; private set; }
    /// <summary>Gets the monetary amount of the payment.</summary>
    public decimal Amount { get; private set; }
    /// <summary>Gets the ISO 4217 currency code for this payment.</summary>
    public string Currency { get; private set; }
    /// <summary>Gets the payment method used (e.g. <c>"CreditCard"</c>, <c>"BankTransfer"</c>).</summary>
    public string Method { get; private set; }
    /// <summary>Gets the current status of the payment.</summary>
    public PaymentStatus Status { get; private set; }
    /// <summary>Gets the date and time the payment was received.</summary>
    public DateTime PaidAt { get; private set; }
    /// <summary>Gets the optional identifier from an external payment provider (e.g. Stripe charge ID).</summary>
    public string? ExternalPaymentId { get; private set; }

    // Navigation
    /// <summary>Gets the parent invoice navigation property.</summary>
    public Invoice? Invoice { get; private set; }
}

/// <summary>
/// Payment status enum
/// </summary>
public enum PaymentStatus
{
    /// <summary>The payment has been initiated but not yet confirmed.</summary>
    Pending,
    /// <summary>The payment has been successfully processed.</summary>
    Completed,
    /// <summary>The payment attempt failed.</summary>
    Failed,
    /// <summary>The payment has been refunded to the payer.</summary>
    Refunded
}
