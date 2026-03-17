using MyApp.Crm.Domain.Leads;

namespace MyApp.Crm.Domain.Tests;

public class LeadInvariantsTests
{
    [Fact]
    public void Lead_Qualify_SetsCustomerIdAndStatus()
    {
        var lead = new Lead(Guid.NewGuid(), "Lead title", "owner");

        lead.Qualify(Guid.NewGuid());

        Assert.Equal(LeadStatus.Qualified, lead.Status);
        Assert.NotNull(lead.CustomerId);
        Assert.NotEqual(Guid.Empty, lead.CustomerId);
    }

    [Fact]
    public void Lead_UpdateDetails_AfterQualify_Throws()
    {
        var lead = new Lead(Guid.NewGuid(), "Lead title", "owner");
        lead.Qualify(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            lead.UpdateDetails("new title", null, null, null, null));
    }
}

