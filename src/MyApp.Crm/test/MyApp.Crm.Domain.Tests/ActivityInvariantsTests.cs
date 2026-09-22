using MyApp.Crm.Domain.Activities;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Activity entity.</summary>
public class ActivityInvariantsTests
{
    /// <summary>Verifies that an activity must be linked to exactly one parent entity (lead, opportunity, or customer).</summary>
    [Fact]
    public void Activity_MustHaveExactlyOneParent()
    {
        Assert.Throws<ArgumentException>(() =>
            new Activity(Guid.NewGuid(), "Call", ActivityType.Call, DateTimeOffset.UtcNow.AddDays(1), "user",
                leadId: null, opportunityId: null, customerId: null));

        Assert.Throws<ArgumentException>(() =>
            new Activity(Guid.NewGuid(), "Call", ActivityType.Call, DateTimeOffset.UtcNow.AddDays(1), "user",
                leadId: Guid.NewGuid(), opportunityId: Guid.NewGuid(), customerId: null));
    }
}

