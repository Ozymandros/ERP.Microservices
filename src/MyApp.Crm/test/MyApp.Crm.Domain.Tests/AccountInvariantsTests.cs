using MyApp.Crm.Domain.Accounts;

namespace MyApp.Crm.Domain.Tests;

public class AccountInvariantsTests
{
    [Fact]
    public void Account_Ctor_EmptyCustomerId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.NewGuid(), Guid.Empty, "Name"));
    }

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

