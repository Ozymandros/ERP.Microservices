using FluentAssertions;
using MyApp.Crm.Domain.Accounts;
using MyApp.Crm.Domain.Activities;
using MyApp.Crm.Domain.Leads;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Crm.Infrastructure.Data.Repositories;
using MyApp.Crm.Infrastructure.Tests.Helpers;
using Xunit;

namespace MyApp.Crm.Infrastructure.Tests.Repositories;

public class CrmRepositoryTests
{
    [Fact]
    public async Task ContactRepository_ListByAccount_OrdersPrimaryFirst()
    {
        using var ctx = CrmTestDbContextFactory.Create();
        var accountId = Guid.NewGuid();
        var primary = new Contact(Guid.NewGuid(), accountId, "Primary Person", isPrimary: true);
        var secondary = new Contact(Guid.NewGuid(), accountId, "Secondary Person");
        ctx.Contacts.AddRange(primary, secondary);
        await ctx.SaveChangesAsync();
        var sut = new ContactRepository(ctx);

        var result = (await sut.ListByAccountAsync(accountId)).ToList();

        result.Should().HaveCount(2);
        result[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task AccountRepository_GetById_ReturnsAccount()
    {
        using var ctx = CrmTestDbContextFactory.Create();
        var account = new Account(Guid.NewGuid(), Guid.NewGuid(), "Acme Corp");
        ctx.Accounts.Add(account);
        await ctx.SaveChangesAsync();
        var sut = new AccountRepository(ctx);

        var result = await sut.GetByIdAsync(account.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task LeadRepository_AddAndGet_PersistsLead()
    {
        using var ctx = CrmTestDbContextFactory.Create();
        var lead = new Lead(Guid.NewGuid(), "New Lead", "owner");
        var sut = new LeadRepository(ctx);
        await sut.AddAsync(lead);
        await ctx.SaveChangesAsync();

        var loaded = await sut.GetByIdAsync(lead.Id);

        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("New Lead");
        loaded.OwnerUsername.Should().Be("owner");
    }

    [Fact]
    public async Task OpportunityRepository_ListByAccount_FiltersCorrectly()
    {
        using var ctx = CrmTestDbContextFactory.Create();
        var customerId = Guid.NewGuid();
        var opp = new Opportunity(Guid.NewGuid(), customerId, "Big deal", "owner");
        ctx.Opportunities.Add(opp);
        await ctx.SaveChangesAsync();
        var sut = new OpportunityRepository(ctx);

        var result = (await sut.ListForForecastAsync("owner", null, null)).ToList();

        result.Should().ContainSingle(o => o.Id == opp.Id);
    }

    [Fact]
    public async Task ActivityRepository_ListByRelated_ReturnsActivities()
    {
        using var ctx = CrmTestDbContextFactory.Create();
        var activity = new Activity(
            Guid.NewGuid(), "Follow up", ActivityType.Call, DateTimeOffset.UtcNow, "owner", customerId: Guid.NewGuid());
        ctx.Activities.Add(activity);
        await ctx.SaveChangesAsync();
        var sut = new ActivityRepository(ctx);

        var result = (await sut.ListAsync()).ToList();

        result.Should().ContainSingle(a => a.Subject == "Follow up");
    }
}