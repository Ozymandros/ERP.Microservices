using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Domain.Tests;

public class OpportunityLineInvariantsTests
{
    [Fact]
    public void OpportunityLine_DiscountOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpportunityLine(Guid.NewGuid(), Guid.NewGuid(), "Desc", 1m, 10m, -0.01m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpportunityLine(Guid.NewGuid(), Guid.NewGuid(), "Desc", 1m, 10m, 1.01m));
    }

    [Fact]
    public void OpportunityLine_QuantityNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpportunityLine(Guid.NewGuid(), Guid.NewGuid(), "Desc", 0m, 10m, 0m));
    }
}

