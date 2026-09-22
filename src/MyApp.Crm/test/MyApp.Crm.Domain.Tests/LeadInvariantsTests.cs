using MyApp.Crm.Domain.Leads;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Lead entity.</summary>
public class LeadInvariantsTests
{
    /// <summary>Verifies that qualifying a lead sets the customer ID and changes status to Qualified.</summary>
    [Fact]
    public void Lead_Qualify_SetsCustomerIdAndStatus()
    {
        var lead = new Lead(Guid.NewGuid(), "Lead title", "owner");

        lead.Qualify(Guid.NewGuid());

        Assert.Equal(LeadStatus.Qualified, lead.Status);
        Assert.NotNull(lead.CustomerId);
        Assert.NotEqual(Guid.Empty, lead.CustomerId);
    }

    /// <summary>Verifies that updating lead details after the lead has been qualified throws an InvalidOperationException.</summary>
    [Fact]
    public void Lead_UpdateDetails_AfterQualify_Throws()
    {
        var lead = new Lead(Guid.NewGuid(), "Lead title", "owner");
        lead.Qualify(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            lead.UpdateDetails("new title", null, null, null, null));
    }
}

