using MyApp.Crm.Domain.Accounts;

namespace MyApp.Crm.Domain.Tests;

/// <summary>Tests for domain invariants enforced by the Contact entity.</summary>
public class ContactInvariantsTests
{
    [Fact]
    public void Contact_Ctor_EmptyAccountId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Contact(Guid.NewGuid(), Guid.Empty, "Jane"));
    }

    [Fact]
    public void Contact_Ctor_BlankFullName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Contact(Guid.NewGuid(), Guid.NewGuid(), "  "));
    }

    [Fact]
    public void Contact_Update_TrimsAndUpdatesFields()
    {
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), "Jane");

        contact.Update(" Jane Doe ", " jane@x.com ", " 123 ", " CTO ");

        Assert.Equal("Jane Doe", contact.FullName);
        Assert.Equal("jane@x.com", contact.Email);
        Assert.Equal("123", contact.Phone);
        Assert.Equal("CTO", contact.Title);
    }

    [Fact]
    public void Contact_Deactivate_ClearsPrimaryAndActive()
    {
        var contact = new Contact(Guid.NewGuid(), Guid.NewGuid(), "Jane", isPrimary: true);

        contact.Deactivate();

        Assert.False(contact.IsActive);
        Assert.False(contact.IsPrimary);
    }

    [Fact]
    public void Account_SetPrimaryContact_UnsetsOthers()
    {
        var account = new Account(Guid.NewGuid(), Guid.NewGuid(), "Acme");
        var c1 = account.AddContact(Guid.NewGuid(), "A", null, null, null, isPrimary: true);
        var c2 = account.AddContact(Guid.NewGuid(), "B", null, null, null, isPrimary: false);

        account.SetPrimaryContact(c2.Id);

        Assert.False(c1.IsPrimary);
        Assert.True(c2.IsPrimary);
    }
}
