using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the OpportunityLine entity.</summary>
public class OpportunityLineInvariantsTests
{
    /// <summary>Verifies that creating an opportunity line with a discount outside 0–1 throws an ArgumentOutOfRangeException.</summary>
    [Fact]
    public void OpportunityLine_DiscountOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpportunityLine(Guid.NewGuid(), Guid.NewGuid(), "Desc", 1m, 10m, -0.01m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpportunityLine(Guid.NewGuid(), Guid.NewGuid(), "Desc", 1m, 10m, 1.01m));
    }

    /// <summary>Verifies that creating an opportunity line with a non-positive quantity throws an ArgumentOutOfRangeException.</summary>
    [Fact]
    public void OpportunityLine_QuantityNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OpportunityLine(Guid.NewGuid(), Guid.NewGuid(), "Desc", 0m, 10m, 0m));
    }
}

