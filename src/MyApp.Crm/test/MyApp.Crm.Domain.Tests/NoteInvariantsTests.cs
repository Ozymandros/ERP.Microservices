using MyApp.Crm.Domain.Notes;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Note entity.</summary>
public class NoteInvariantsTests
{
    [Fact]
    public void ForLead_BlankBody_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Note.ForLead(Guid.NewGuid(), "  ", Guid.NewGuid()));
    }

    [Fact]
    public void ForLead_TrimsBodyAndSetsLeadId()
    {
        var leadId = Guid.NewGuid();

        var note = Note.ForLead(Guid.NewGuid(), " hello ", leadId);

        Assert.Equal("hello", note.Body);
        Assert.Equal(leadId, note.LeadId);
        Assert.Null(note.OpportunityId);
        Assert.Null(note.ActivityId);
    }

    [Fact]
    public void ForOpportunity_SetsOpportunityId()
    {
        var opportunityId = Guid.NewGuid();

        var note = Note.ForOpportunity(Guid.NewGuid(), "opp note", opportunityId);

        Assert.Equal(opportunityId, note.OpportunityId);
        Assert.Null(note.LeadId);
    }

    [Fact]
    public void ForActivity_SetsActivityId()
    {
        var activityId = Guid.NewGuid();

        var note = Note.ForActivity(Guid.NewGuid(), "activity note", activityId);

        Assert.Equal(activityId, note.ActivityId);
        Assert.Null(note.LeadId);
    }

    [Fact]
    public void UpdateBody_Blank_Throws()
    {
        var note = Note.ForLead(Guid.NewGuid(), "body", Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => note.UpdateBody(" "));
    }

    [Fact]
    public void UpdateBody_TrimsValue()
    {
        var note = Note.ForLead(Guid.NewGuid(), "old", Guid.NewGuid());

        note.UpdateBody(" new body ");

        Assert.Equal("new body", note.Body);
    }
}
