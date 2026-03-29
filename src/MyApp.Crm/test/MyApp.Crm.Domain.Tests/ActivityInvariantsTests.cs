using MyApp.Crm.Domain.Activities;

namespace MyApp.Crm.Domain.Tests;

public class ActivityInvariantsTests
{
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

