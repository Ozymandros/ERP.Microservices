using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Opportunity entity.</summary>
public class OpportunityInvariantsTests
{
    /// <summary>Verifies that updating forecast with a probability outside 0–1 throws an ArgumentOutOfRangeException.</summary>
    [Fact]
    public void Opportunity_UpdateForecast_ProbabilityOutOfRange_Throws()
    {
        var opp = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner");
        Assert.Throws<ArgumentOutOfRangeException>(() => opp.UpdateForecast(-0.1m, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => opp.UpdateForecast(1.1m, null, null));
    }

    /// <summary>Verifies that attempting to move stage after an opportunity is won throws an InvalidOperationException.</summary>
    [Fact]
    public void Opportunity_MoveToStage_AfterWon_Throws()
    {
        var opp = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner");
        opp.MarkWon();

        Assert.Throws<InvalidOperationException>(() => opp.MoveToStage(OpportunityStage.Proposal));
    }
}

