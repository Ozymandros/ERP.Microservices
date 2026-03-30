using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Domain.Tests;

public class OpportunityInvariantsTests
{
    [Fact]
    public void Opportunity_UpdateForecast_ProbabilityOutOfRange_Throws()
    {
        var opp = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner");
        Assert.Throws<ArgumentOutOfRangeException>(() => opp.UpdateForecast(-0.1m, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => opp.UpdateForecast(1.1m, null, null));
    }

    [Fact]
    public void Opportunity_MoveToStage_AfterWon_Throws()
    {
        var opp = new Opportunity(Guid.NewGuid(), Guid.NewGuid(), "Opp", "owner");
        opp.MarkWon();

        Assert.Throws<InvalidOperationException>(() => opp.MoveToStage(OpportunityStage.Proposal));
    }
}

