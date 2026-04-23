using FluentAssertions;
using MyApp.Billing.Domain.Entities;
using Xunit;

namespace MyApp.Billing.Domain.Tests.Entities;

public class InvoiceTests
{
    [Fact]
    public void Constructor_CreatesInvoiceWithDraftStatus()
    {
        // Arrange & Act
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.OutstandingAmount.Should().Be(0);
        invoice.Lines.Should().BeEmpty();
        invoice.Payments.Should().BeEmpty();
    }

    [Fact]
    public void AddLine_WhenDraft_AddsLineAndRecalculatesTotals()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");

        // Act
        invoice.AddLine("Test Item", 2, 100m, 10m, 10m);

        // Assert
        invoice.Lines.Count.Should().Be(1);
        invoice.TotalNet.Should().Be(190m); // 2*100 - 10
        invoice.TotalTax.Should().Be(19m);  // 190 * 10%
        invoice.TotalGross.Should().Be(209m);
        invoice.OutstandingAmount.Should().Be(209m);
    }

    [Fact]
    public void AddLine_WhenIssued_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.Issue("INV-001", DateTime.UtcNow, 30);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            invoice.AddLine("Test Item", 1, 50m, 10m));
    }

    [Fact]
    public void Issue_WithValidData_SetsStatusToIssued()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.AddLine("Test Item", 1, 100m, 10m);
        var issueDate = DateTime.UtcNow;

        // Act
        invoice.Issue("INV-001", issueDate, 30);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.InvoiceNumber.Should().Be("INV-001");
        invoice.IssueDate.Should().Be(issueDate);
        invoice.DueDate.Should().BeCloseTo(issueDate.AddDays(30), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Issue_WithoutLines_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            invoice.Issue("INV-001", DateTime.UtcNow, 30));
    }

    [Fact]
    public void RecordPayment_ValidPayment_ReducesOutstandingAmount()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.AddLine("Test Item", 1, 100m, 0m);
        invoice.Issue("INV-001", DateTime.UtcNow, 30);
        var initialOutstanding = invoice.OutstandingAmount;

        // Act
        invoice.RecordPayment(50m, "CreditCard", DateTime.UtcNow);

        // Assert
        invoice.OutstandingAmount.Should().Be(initialOutstanding - 50m);
        invoice.Payments.Count.Should().Be(1);
    }

    [Fact]
    public void RecordPayment_FullPayment_SetsStatusToPaid()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.AddLine("Test Item", 1, 100m, 0m);
        invoice.Issue("INV-001", DateTime.UtcNow, 30);

        // Act
        invoice.RecordPayment(100m, "CreditCard", DateTime.UtcNow);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.OutstandingAmount.Should().Be(0);
    }

    [Fact]
    public void MarkAsPaid_WhenOutstandingGreaterThanZero_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.AddLine("Test Item", 1, 100m, 0m);
        invoice.Issue("INV-001", DateTime.UtcNow, 30);
        invoice.RecordPayment(50m, "CreditCard", DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => invoice.MarkAsPaid());
    }

    [Fact]
    public void Cancel_WhenPaid_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.AddLine("Test Item", 1, 100m, 0m);
        invoice.Issue("INV-001", DateTime.UtcNow, 30);
        invoice.RecordPayment(100m, "CreditCard", DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => invoice.Cancel());
    }

    [Fact]
    public void CreateCreditNote_ForIssuedInvoice_CreatesCreditNoteAndAdjustsOutstanding()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");
        invoice.AddLine("Test Item", 2, 100m, 10m);
        invoice.Issue("INV-001", DateTime.UtcNow, 30);
        var initialOutstanding = invoice.OutstandingAmount;

        var lines = new List<CreditNoteLineData>
        {
            new CreditNoteLineData("Refund Item", 1, 50m, 10m, 0)
        };

        // Act
        var creditNote = invoice.CreateCreditNote(lines, "Customer request");

        // Assert
        creditNote.Should().NotBeNull();
        creditNote.OriginalInvoiceId.Should().Be(invoice.Id);
        invoice.CreditNotes.Count.Should().Be(1);
        // Outstanding should be reduced by the credit note amount
        invoice.OutstandingAmount.Should().BeLessThan(initialOutstanding);
    }

    [Fact]
    public void RecalculateTotals_AfterMultipleLines_CalculatesCorrectly()
    {
        // Arrange
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "USD");

        // Act
        invoice.AddLine("Item 1", 2, 50m, 10m);   // Net: 100, Tax: 10, Gross: 110
        invoice.AddLine("Item 2", 1, 200m, 5m);  // Net: 200, Tax: 10, Gross: 210

        // Assert
        invoice.TotalNet.Should().Be(300m);
        invoice.TotalTax.Should().Be(20m);
        invoice.TotalGross.Should().Be(320m);
    }
}
