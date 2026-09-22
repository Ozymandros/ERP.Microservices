using MyApp.Crm.Domain.Accounts;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Account entity.</summary>
public class AccountInvariantsTests
{
    /// <summary>Verifies that constructing an Account with an empty customer ID throws an ArgumentException.</summary>
    [Fact]
    public void Account_Ctor_EmptyCustomerId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.NewGuid(), Guid.Empty, "Name"));
    }

    /// <summary>Verifies that adding a new primary contact clears the primary flag from other contacts.</summary>
    [Fact]
    public void Account_AddContact_Primary_UnsetsOtherPrimaryContacts()
    {
        var account = new Account(Guid.NewGuid(), Guid.NewGuid(), "Acme");

        var c1 = account.AddContact(Guid.NewGuid(), "A", null, null, null, isPrimary: true);
        var c2 = account.AddContact(Guid.NewGuid(), "B", null, null, null, isPrimary: true);

        Assert.False(c1.IsPrimary);
        Assert.True(c2.IsPrimary);
    }
}

