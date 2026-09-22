using FluentAssertions;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Infrastructure.Persistence;
using MyApp.Billing.Infrastructure.Repositories;
using MyApp.Billing.Infrastructure.Tests.Helpers;
using Xunit;

namespace MyApp.Billing.Infrastructure.Tests.Repositories;

public class CreditNoteRepositoryTests
{
    private readonly BillingDbContext _context;
    private readonly CreditNoteRepository _repository;

    public CreditNoteRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new CreditNoteRepository(_context);
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Seeds a persisted issued invoice so credit notes can reference a valid FK.</summary>
    private Invoice SeedIssuedInvoice(string? invoiceNumber = null)
    {
        var inv = new Invoice(Guid.NewGuid(), $"INV-CN-{Guid.NewGuid().ToString()[..8]}", Guid.NewGuid(), "USD");
        inv.AddLine("Widget", 2, 100m, 10m);
        inv.Issue(invoiceNumber ?? $"INV-{Guid.NewGuid().ToString()[..8]}", DateTime.UtcNow, 30);
        _context.Invoices.Add(inv);
        _context.SaveChanges();
        return inv;
    }

    private CreditNote AddCreditNote(Guid originalInvoiceId, string reason = "Returned goods",
        int lineQty = 1, decimal linePrice = 50m)
    {
        var lines = new List<CreditNoteLineData>
        {
            new(reason, lineQty, linePrice, 10m, 0m)
        };
        var cn = new CreditNote(Guid.NewGuid(), originalInvoiceId, lines, reason);
        _context.CreditNotes.Add(cn);
        _context.SaveChanges();
        return cn;
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsCreditNote()
    {
        var inv = SeedIssuedInvoice();
        var cn = AddCreditNote(inv.Id);

        var result = await _repository.GetByIdAsync(cn.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(cn.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ─── GetByInvoiceIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByInvoiceIdAsync_ReturnsOnlyThatInvoicesCreditNotes()
    {
        var inv1 = SeedIssuedInvoice();
        var inv2 = SeedIssuedInvoice();

        AddCreditNote(inv1.Id, "Return A");
        AddCreditNote(inv1.Id, "Return B");
        AddCreditNote(inv2.Id, "Return C");   // excluded

        var result = await _repository.GetByInvoiceIdAsync(inv1.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(cn => cn.OriginalInvoiceId.Should().Be(inv1.Id));
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_IncludesLines()
    {
        var inv = SeedIssuedInvoice();
        AddCreditNote(inv.Id);

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        result.Should().HaveCount(1);
        result[0].Lines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_NoMatches_ReturnsEmptyList()
    {
        var result = await _repository.GetByInvoiceIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_PreservesReasonAndTotals()
    {
        var inv = SeedIssuedInvoice();
        AddCreditNote(inv.Id, "Damaged item", lineQty: 2, linePrice: 75m);

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        var cn = result.Single();
        cn.Reason.Should().Be("Damaged item");
        cn.TotalGross.Should().BePositive();
        cn.Status.Should().Be(CreditNoteStatus.Issued);
    }

    [Fact]
    public async Task GetByInvoiceIdAsync_LineFieldsMappedCorrectly()
    {
        var inv = SeedIssuedInvoice();
        AddCreditNote(inv.Id, reason: "Partial refund", lineQty: 3, linePrice: 40m);

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        var line = result.Single().Lines.Single();
        line.Quantity.Should().Be(3);
        line.UnitPrice.Should().Be(40m);
        // LineNet = 3 * 40 - 0 = 120, LineTax = 120 * 10% = 12, LineGross = 132
        line.LineNet.Should().Be(120m);
        line.LineTax.Should().Be(12m);
        line.LineGross.Should().Be(132m);
    }

    // ─── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsCreditNoteWithLines()
    {
        var inv = SeedIssuedInvoice();
        var lines = new List<CreditNoteLineData> { new("Refund", 1, 80m, 5m, 0m) };
        var cn = new CreditNote(Guid.NewGuid(), inv.Id, lines, "Product defect");

        await _repository.AddAsync(cn);

        var stored = await _context.CreditNotes.FindAsync(cn.Id);
        stored.Should().NotBeNull();
        stored!.Reason.Should().Be("Product defect");
        stored.OriginalInvoiceId.Should().Be(inv.Id);
    }

    [Fact]
    public async Task AddAsync_SetsAuditFields()
    {
        var inv = SeedIssuedInvoice();
        var cn = new CreditNote(Guid.NewGuid(), inv.Id,
            new List<CreditNoteLineData> { new("X", 1, 10m, 0m, 0m) },
            "Test");

        await _repository.AddAsync(cn);
        await _context.SaveChangesAsync();

        var stored = await _context.CreditNotes.FindAsync(cn.Id);
        stored!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        stored.CreatedBy.Should().Be("SystemUser");
    }

    [Fact]
    public async Task AddAsync_PersistsIssuedStatus()
    {
        var inv = SeedIssuedInvoice();
        var cn = new CreditNote(Guid.NewGuid(), inv.Id,
            new List<CreditNoteLineData> { new("Item", 1, 10m, 0m, 0m) },
            "reason");

        await _repository.AddAsync(cn);

        var stored = await _context.CreditNotes.FindAsync(cn.Id);
        stored!.Status.Should().Be(CreditNoteStatus.Issued);
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AfterCancel_PersistsCancelledStatus()
    {
        var inv = SeedIssuedInvoice();
        var cn = AddCreditNote(inv.Id);
        cn.Cancel();

        await _repository.UpdateAsync(cn);

        var stored = await _context.CreditNotes.FindAsync(cn.Id);
        stored!.Status.Should().Be(CreditNoteStatus.Cancelled);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesCreditNote()
    {
        var inv = SeedIssuedInvoice();
        var cn = AddCreditNote(inv.Id);

        await _repository.DeleteAsync(cn);
        await _context.SaveChangesAsync();

        var stored = await _context.CreditNotes.FindAsync(cn.Id);
        stored.Should().BeNull();
    }

    // ─── cross-contamination / isolation ─────────────────────────────────────

    [Fact]
    public async Task GetByInvoiceIdAsync_MultipleInvoices_DoesNotCrossContaminate()
    {
        var inv1 = SeedIssuedInvoice();
        var inv2 = SeedIssuedInvoice();
        var inv3 = SeedIssuedInvoice();

        AddCreditNote(inv1.Id);
        AddCreditNote(inv2.Id);
        AddCreditNote(inv2.Id);

        var r1 = await _repository.GetByInvoiceIdAsync(inv1.Id);
        var r2 = await _repository.GetByInvoiceIdAsync(inv2.Id);
        var r3 = await _repository.GetByInvoiceIdAsync(inv3.Id);

        r1.Should().HaveCount(1);
        r2.Should().HaveCount(2);
        r3.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_MultipleCreditNotesForSameInvoice_AllPersisted()
    {
        var inv = SeedIssuedInvoice();

        AddCreditNote(inv.Id, "Return 1");
        AddCreditNote(inv.Id, "Return 2");
        AddCreditNote(inv.Id, "Return 3");

        var result = await _repository.GetByInvoiceIdAsync(inv.Id);

        result.Should().HaveCount(3);
        result.Select(cn => cn.Reason).Should().BeEquivalentTo(
            new[] { "Return 1", "Return 2", "Return 3" });
    }
}
