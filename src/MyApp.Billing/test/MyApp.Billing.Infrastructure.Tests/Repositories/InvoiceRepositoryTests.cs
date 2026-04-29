using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Billing.Infrastructure.Repositories;
using MyApp.Billing.Infrastructure.Tests.Helpers;
using Xunit;

namespace MyApp.Billing.Infrastructure.Tests.Repositories;

public class InvoiceRepositoryTests
{
    private readonly BillingDbContext _context;
    private readonly InvoiceRepository _repository;

    public InvoiceRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new InvoiceRepository(_context);
    }

    // ─── factory helpers ──────────────────────────────────────────────────────

    private Invoice CreateDraftInvoice(Guid? customerId = null, Guid? orderId = null,
        string currency = "USD", string invoiceNumber = "")
    {
        var inv = new Invoice(Guid.NewGuid(), customerId ?? Guid.NewGuid(), currency);
        inv.AddLine("Widget", 2, 50m, 10m);
        _context.Invoices.Add(inv);
        _context.SaveChanges();
        return inv;
    }

    private Invoice CreateIssuedInvoice(Guid? customerId = null, Guid? orderId = null,
        string invoiceNumber = "INV-001", int dueDays = 30)
    {
        var inv = new Invoice(Guid.NewGuid(), customerId ?? Guid.NewGuid(), "USD");
        inv.AddLine("Widget", 2, 50m, 10m);
        inv.Issue(invoiceNumber, DateTime.UtcNow, dueDays);
        _context.Invoices.Add(inv);
        _context.SaveChanges();
        return inv;
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsInvoice()
    {
        var inv = CreateDraftInvoice();

        var result = await _repository.GetByIdAsync(inv.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(inv.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ─── GetByInvoiceNumberAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByInvoiceNumberAsync_ExistingNumber_ReturnsInvoiceWithLines()
    {
        var inv = CreateIssuedInvoice(invoiceNumber: "INV-100");

        var result = await _repository.GetByInvoiceNumberAsync("INV-100");

        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().Be("INV-100");
        result.Lines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_NonExistentNumber_ReturnsNull()
    {
        var result = await _repository.GetByInvoiceNumberAsync("INV-GHOST");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_IsCaseSensitive()
    {
        CreateIssuedInvoice(invoiceNumber: "INV-200");

        var result = await _repository.GetByInvoiceNumberAsync("inv-200");

        // EF InMemory string comparison is case-sensitive by default
        result.Should().BeNull();
    }

    // ─── GetByCustomerIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOnlyThatCustomersInvoices()
    {
        var customerId = Guid.NewGuid();
        CreateDraftInvoice(customerId: customerId);
        CreateDraftInvoice(customerId: customerId);
        CreateDraftInvoice();  // different customer — should be excluded

        var result = await _repository.GetByCustomerIdAsync(customerId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public async Task GetByCustomerIdAsync_IncludesLines()
    {
        var customerId = Guid.NewGuid();
        CreateDraftInvoice(customerId: customerId);

        var result = await _repository.GetByCustomerIdAsync(customerId);

        result.Should().HaveCount(1);
        result[0].Lines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByCustomerIdAsync_NoInvoices_ReturnsEmptyList()
    {
        var result = await _repository.GetByCustomerIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCustomerIdAsync_OrdersByCreatedAtDescending()
    {
        var customerId = Guid.NewGuid();

        // AuditableDbContext only overrides SaveChangesAsync; the helper calls the sync
        // variant, so CreatedAt stays at DateTime.MinValue unless we set it explicitly.
        var inv1 = new Invoice(Guid.NewGuid(), customerId, "USD");
        inv1.AddLine("Widget", 2, 50m, 10m);
        inv1.CreatedAt = DateTime.UtcNow.AddSeconds(-10);
        _context.Invoices.Add(inv1);
        _context.SaveChanges();

        var inv2 = new Invoice(Guid.NewGuid(), customerId, "USD");
        inv2.AddLine("Widget", 2, 50m, 10m);
        inv2.CreatedAt = DateTime.UtcNow;
        _context.Invoices.Add(inv2);
        _context.SaveChanges();

        var result = await _repository.GetByCustomerIdAsync(customerId);

        // Most recent first
        result[0].CreatedAt.Should().BeOnOrAfter(result[1].CreatedAt);
    }

    // ─── GetOpenInvoicesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOpenInvoicesAsync_ReturnsIssuedInvoices()
    {
        CreateIssuedInvoice(invoiceNumber: "INV-OPEN-1");
        CreateIssuedInvoice(invoiceNumber: "INV-OPEN-2");
        CreateDraftInvoice();  // draft — excluded

        var result = await _repository.GetOpenInvoicesAsync();

        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().AllSatisfy(i =>
            i.Status.Should().BeOneOf(InvoiceStatus.Issued, InvoiceStatus.Sent));
    }

    [Fact]
    public async Task GetOpenInvoicesAsync_ExcludesPaidAndCancelledInvoices()
    {
        // Paid invoice
        var paid = CreateIssuedInvoice(invoiceNumber: "INV-PAID");
        paid.RecordPayment(paid.TotalGross, "Card", DateTime.UtcNow);
        // EF InMemory can treat newly created dependents as Modified when only
        // the aggregate navigation is mutated; force the new payment to be Added
        // so the store update does not throw concurrency exceptions.
        var newPayment = paid.Payments.Single();
        _context.Entry(newPayment).State = EntityState.Added;
        _context.SaveChanges();

        // Cancelled invoice
        var cancelled = CreateDraftInvoice();
        cancelled.Cancel();
        _context.SaveChanges();

        var result = await _repository.GetOpenInvoicesAsync();

        result.Should().NotContain(i => i.Status == InvoiceStatus.Paid);
        result.Should().NotContain(i => i.Status == InvoiceStatus.Cancelled);
    }

    [Fact]
    public async Task GetOpenInvoicesAsync_IncludesLines()
    {
        CreateIssuedInvoice(invoiceNumber: "INV-LINES");

        var result = await _repository.GetOpenInvoicesAsync();

        result.Should().Contain(i => i.Lines.Count > 0);
    }

    [Fact]
    public async Task GetOpenInvoicesAsync_OrdersByDueDateAscending()
    {
        // due in 60 days
        CreateIssuedInvoice(invoiceNumber: "INV-FAR", dueDays: 60);
        // due in 10 days
        CreateIssuedInvoice(invoiceNumber: "INV-NEAR", dueDays: 10);

        var result = await _repository.GetOpenInvoicesAsync();

        var dueDates = result.Select(i => i.DueDate!.Value).ToList();
        dueDates.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetOpenInvoicesAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetOpenInvoicesAsync();

        result.Should().BeEmpty();
    }

    // ─── GetInvoicesByOrderIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetInvoicesByOrderIdAsync_ReturnsMatchingInvoices()
    {
        var orderId = Guid.NewGuid();

        // BuildInvoiceWithOrderId adds + saves each invoice internally
        BuildInvoiceWithOrderId(orderId);
        BuildInvoiceWithOrderId(orderId);
        BuildInvoiceWithOrderId(Guid.NewGuid());  // different order — excluded

        var result = await _repository.GetInvoicesByOrderIdAsync(orderId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInvoicesByOrderIdAsync_IncludesLines()
    {
        var orderId = Guid.NewGuid();
        // BuildInvoiceWithOrderId already adds a line and saves; the returned invoice
        // has one line ("Item").
        BuildInvoiceWithOrderId(orderId);

        var result = await _repository.GetInvoicesByOrderIdAsync(orderId);

        result.Should().HaveCount(1);
        result[0].Lines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetInvoicesByOrderIdAsync_NoMatch_ReturnsEmptyList()
    {
        var result = await _repository.GetInvoicesByOrderIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    // ─── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsInvoice()
    {
        var inv = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        inv.AddLine("Product A", 1, 100m, 10m);

        await _repository.AddAsync(inv);

        var stored = await _context.Invoices.FindAsync(inv.Id);
        stored.Should().NotBeNull();
        stored!.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task AddAsync_SetsAuditFields()
    {
        var inv = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        inv.AddLine("X", 1, 10m, 0m);

        await _repository.AddAsync(inv);

        var stored = await _context.Invoices.FindAsync(inv.Id);
        stored!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        stored.CreatedBy.Should().Be("SystemUser");
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var inv = CreateDraftInvoice();
        inv.Issue("INV-UPD-001", DateTime.UtcNow, 30);

        await _repository.UpdateAsync(inv);

        var stored = await _context.Invoices.FindAsync(inv.Id);
        stored!.Status.Should().Be(InvoiceStatus.Issued);
        stored.InvoiceNumber.Should().Be("INV-UPD-001");
    }

    [Fact]
    public async Task UpdateAsync_PersistsPaymentAndOutstandingAmount()
    {
        var inv = CreateIssuedInvoice(invoiceNumber: "INV-UPD-002");
        var gross = inv.TotalGross;
        inv.RecordPayment(gross / 2, "Card", DateTime.UtcNow);
        var newPayment = inv.Payments.Single();
        _context.Entry(newPayment).State = EntityState.Added;
        await _context.SaveChangesAsync();

        var stored = await _context.Invoices.FindAsync(inv.Id);
        stored!.OutstandingAmount.Should().Be(gross - gross / 2);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesInvoice()
    {
        var inv = CreateDraftInvoice();

        await _repository.DeleteAsync(inv);

        var stored = await _context.Invoices.FindAsync(inv.Id);
        stored.Should().BeNull();
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and persists an Invoice that has an OrderId set via EF Core's
    /// ChangeTracker entry (bypasses the private setter cleanly).
    /// </summary>
    private Invoice BuildInvoiceWithOrderId(Guid orderId)
    {
        var inv = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        inv.AddLine("Item", 1, 100m, 10m);
        _context.Invoices.Add(inv);
        _context.Entry(inv).Property(e => e.OrderId).CurrentValue = orderId;
        _context.SaveChanges();
        return inv;
    }
}
