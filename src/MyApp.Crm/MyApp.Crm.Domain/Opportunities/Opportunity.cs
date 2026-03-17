using MyApp.Crm.Domain.Notes;
using MyApp.Crm.Domain.Tags;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Opportunities;

public class Opportunity(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid CustomerId { get; private set; }
    public Guid? LeadId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public OpportunityStage Stage { get; private set; } = OpportunityStage.Prospecting;
    public decimal Probability { get; private set; } // 0..1
    public decimal? ExpectedAmount { get; private set; }
    public DateOnly? ExpectedCloseDate { get; private set; }

    public string OwnerUsername { get; private set; } = string.Empty;

    public Guid? ConvertedSalesQuoteId { get; private set; }
    public string? ConvertedSalesQuoteNumber { get; private set; }

    public List<Note> Notes { get; private set; } = new();
    public List<OpportunityTag> Tags { get; private set; } = new();

    public Opportunity(
        Guid id,
        Guid customerId,
        string name,
        string ownerUsername,
        Guid? leadId = null) : this(id)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        CustomerId = customerId;
        LeadId = leadId;
        Name = NormalizeRequired(name, nameof(name));
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
        Stage = OpportunityStage.Prospecting;
        Probability = 0m;
    }

    public void UpdateForecast(decimal probability, decimal? expectedAmount, DateOnly? expectedCloseDate)
    {
        EnsureNotClosed();

        if (probability is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 1.");

        if (expectedAmount is < 0m)
            throw new ArgumentOutOfRangeException(nameof(expectedAmount), "Expected amount cannot be negative.");

        Probability = probability;
        ExpectedAmount = expectedAmount;
        ExpectedCloseDate = expectedCloseDate;
    }

    public void MoveToStage(OpportunityStage stage)
    {
        EnsureNotClosed();

        if (stage is OpportunityStage.Won or OpportunityStage.Lost)
            throw new InvalidOperationException("Use MarkWon/MarkLost for closing an opportunity.");

        if (stage < OpportunityStage.Prospecting || stage > OpportunityStage.Negotiation)
            throw new ArgumentOutOfRangeException(nameof(stage));

        Stage = stage;
    }

    public void MarkWon(string? note = null)
    {
        EnsureNotClosed();
        Stage = OpportunityStage.Won;
        if (!string.IsNullOrWhiteSpace(note))
        {
            Notes.Add(Note.ForOpportunity(Guid.NewGuid(), note, Id));
        }
    }

    public void SetConvertedQuote(Guid quoteId, string quoteNumber)
    {
        if (quoteId == Guid.Empty) throw new ArgumentException("QuoteId is required.", nameof(quoteId));
        if (string.IsNullOrWhiteSpace(quoteNumber)) throw new ArgumentException("Quote number is required.", nameof(quoteNumber));

        if (ConvertedSalesQuoteId.HasValue)
            throw new InvalidOperationException("Opportunity is already converted.");

        ConvertedSalesQuoteId = quoteId;
        ConvertedSalesQuoteNumber = quoteNumber.Trim();
    }

    public void MarkLost(string reasonNote)
    {
        EnsureNotClosed();
        Stage = OpportunityStage.Lost;
        Notes.Add(Note.ForOpportunity(Guid.NewGuid(), reasonNote, Id));
    }

    private void EnsureNotClosed()
    {
        if (Stage is OpportunityStage.Won or OpportunityStage.Lost)
            throw new InvalidOperationException("Opportunity is closed and cannot be modified.");
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}

