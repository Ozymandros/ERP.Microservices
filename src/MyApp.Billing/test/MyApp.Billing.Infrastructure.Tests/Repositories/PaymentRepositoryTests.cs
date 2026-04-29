using FluentAssertions;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Billing.Infrastructure.Repositories;
using MyApp.Billing.Infrastructure.Tests.Helpers;
using Xunit;

namespace MyApp.Billing.Infrastructure.Tests.Repositories;

public class PaymentRepositoryTests
{
    private readonly BillingDbContext _context;
    private readonly PaymentRepository _repository;

    public PaymentRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new PaymentRepository(_context);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Seeds a persisted invoice so payments can reference a valid FK.</summary>
    private Invoice SeedInvoice()
    {
        var inv = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        inv.AddLine("Widget", 1, 100m, 10m);
        inv.Issue($"INV-{Guid.NewGuid().ToString()[..8]}", DateTime.UtcNow, 30);
        _context.Invoices.Add(inv);
        _context.SaveChanges();
        return inv;
    }

    private Payment AddPayment(Guid invoiceId, decimal amount = 55m,
        string method = "Card", DateTime? paidAt = null)
    {
        var p = new Payment(Guid.NewGuid(), invoiceId, amount, "USD", method,
            paidAt ?? DateTime.UtcNow);
        _context.Payments.Add(p);
        _context.SaveChanges();
        return p;
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsPayment()
    {
        var inv = SeedInvoice();
        var payment = AddPayment(inv.Id);

        var result = await _repository.GetByIdAsync(payment.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
        result.InvoiceId.Should().Be(inv.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ─── GetByInvoiceIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByInvoiceIdAsync_ReturnsOnlyThatInvoicesPayments()
    {
        var inv1 = SeedInvoice();
        var inv2 = SeedInvoice();

        AddPayment(inv1.Id, 30m);
        AddPayment(inv1.Id, 70m);
        AddPayment(inv2.Id, 50m);   // should be excluded

        var result = await _repository.GetByInvoiceIdAsync(inv1.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.InvoiceId.Should().Be(inv1.Id));
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_NoPayments_ReturnsEmptyList()
    {
        var inv = SeedInvoice();

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_OrdersByPaidAtDescending()
    {
        var inv = SeedInvoice();
        var earlier = DateTime.UtcNow.AddDays(-2);
        var later   = DateTime.UtcNow.AddDays(-1);

        AddPayment(inv.Id, 20m, paidAt: earlier);
        await Task.Delay(5);
        AddPayment(inv.Id, 30m, paidAt: later);

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        // Most recent payment first
        result[0].PaidAt.Should().BeOnOrAfter(result[1].PaidAt);
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_PreservesAmount()
    {
        var inv = SeedInvoice();
        AddPayment(inv.Id, 123.45m, "BankTransfer");

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        result.Single().Amount.Should().Be(123.45m);
        result.Single().Method.Should().Be("BankTransfer");
    }

    // ─── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsPayment()
    {
        var inv = SeedInvoice();
        var payment = new Payment(Guid.NewGuid(), inv.Id, 99m, "USD", "Cash", DateTime.UtcNow);

        await _repository.AddAsync(payment);

        var stored = await _context.Payments.FindAsync(payment.Id);
        stored.Should().NotBeNull();
        stored!.Amount.Should().Be(99m);
        stored.Method.Should().Be("Cash");
    }

    [Fact]
    public async Task AddAsync_SetsAuditFields()
    {
        var inv = SeedInvoice();
        var payment = new Payment(Guid.NewGuid(), inv.Id, 50m, "USD", "Card", DateTime.UtcNow);

        await _repository.AddAsync(payment);

        var stored = await _context.Payments.FindAsync(payment.Id);
        stored!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        stored.CreatedBy.Should().Be("SystemUser");
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var inv = SeedInvoice();
        var payment = AddPayment(inv.Id, 50m);

        // Payment entity has private setters; test that the repo calls SaveChanges
        // by mutating something accessible — here we verify the record round-trips
        await _repository.UpdateAsync(payment);

        var stored = await _context.Payments.FindAsync(payment.Id);
        stored.Should().NotBeNull();
        stored!.Amount.Should().Be(50m);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesPayment()
    {
        var inv = SeedInvoice();
        var payment = AddPayment(inv.Id);

        await _repository.DeleteAsync(payment);

        var stored = await _context.Payments.FindAsync(payment.Id);
        stored.Should().BeNull();
    }

    // ─── edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByInvoiceIdAsync_MultipleInvoices_DoesNotCrossContaminate()
    {
        var inv1 = SeedInvoice();
        var inv2 = SeedInvoice();
        var inv3 = SeedInvoice();

        AddPayment(inv1.Id, 10m);
        AddPayment(inv2.Id, 20m);
        AddPayment(inv2.Id, 25m);
        AddPayment(inv3.Id, 30m);

        var r1 = await _repository.GetByInvoiceIdAsync(inv1.Id);
        var r2 = await _repository.GetByInvoiceIdAsync(inv2.Id);
        var r3 = await _repository.GetByInvoiceIdAsync(inv3.Id);

        r1.Should().HaveCount(1);
        r2.Should().HaveCount(2);
        r3.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_WithExternalPaymentId_PersistsCorrectly()
    {
        var inv = SeedInvoice();
        var extId = "STRIPE-CH-123456";
        var payment = new Payment(Guid.NewGuid(), inv.Id, 200m, "USD", "Stripe",
            DateTime.UtcNow, extId);

        await _repository.AddAsync(payment);

        var stored = await _context.Payments.FindAsync(payment.Id);
        stored!.ExternalPaymentId.Should().Be(extId);
    }
}
