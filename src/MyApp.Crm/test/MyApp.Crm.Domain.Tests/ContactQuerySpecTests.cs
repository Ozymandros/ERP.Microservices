using FluentAssertions;
using Xunit;
using MyApp.Crm.Domain.Accounts;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Crm.Domain.Tests;

public class ContactQuerySpecTests
{
  private static IQueryable<Contact> SampleContacts()
  {
    var accountId = Guid.NewGuid();
    return new List<Contact>
    {
      new(Guid.NewGuid(), accountId, "Alice", isPrimary: true),
      new(Guid.NewGuid(), accountId, "Bob", isPrimary: false) { /* inactive via deactivate if needed */ },
      new(Guid.NewGuid(), Guid.NewGuid(), "Carol")
    }.AsQueryable();
  }

  [Fact]
  public void ApplyFilters_AccountId_Filters()
  {
    var accountId = SampleContacts().First().AccountId;
    var spec = new ContactQuerySpec(new QuerySpec
    {
      Filters = new Dictionary<string, string> { { "accountid", accountId.ToString() } }
    });

    var result = spec.ApplyFilters(SampleContacts()).ToList();

    result.Should().HaveCount(2);
    result.Should().OnlyContain(c => c.AccountId == accountId);
  }

  [Fact]
  public void ApplyFilters_IsPrimary_Filters()
  {
    var spec = new ContactQuerySpec(new QuerySpec
    {
      Filters = new Dictionary<string, string> { { "isprimary", "true" } }
    });

    var result = spec.ApplyFilters(SampleContacts()).ToList();

    result.Should().ContainSingle(c => c.IsPrimary);
  }

  [Fact]
  public void ApplyFilters_SearchTerm_MatchesName()
  {
    var spec = new ContactQuerySpec(new QuerySpec { SearchTerm = "Carol" });

    var result = spec.ApplyFilters(SampleContacts()).ToList();

    result.Should().ContainSingle(c => c.FullName == "Carol");
  }
}