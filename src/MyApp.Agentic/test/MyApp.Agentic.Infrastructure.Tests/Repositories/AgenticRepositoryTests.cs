using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Infrastructure.Data.Repositories;
using MyApp.Agentic.Infrastructure.Tests.Helpers;

namespace MyApp.Agentic.Infrastructure.Tests.Repositories;

public class AgenticRepositoryTests
{
    [Fact]
    public async Task AIProviderRepository_GetById_ReturnsProvider()
    {
        await using var ctx = AgenticTestDbContextFactory.Create();
        var (provider, _) = AgenticTestDbContextFactory.SeedProviderAndModel(ctx);
        var sut = new AIProviderRepository(ctx);

        var result = await sut.GetByIdAsync(provider.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("OpenAI");
    }

    [Fact]
    public async Task AIModelRepository_GetByProviderAsync_ReturnsModels()
    {
        await using var ctx = AgenticTestDbContextFactory.Create();
        var (provider, model) = AgenticTestDbContextFactory.SeedProviderAndModel(ctx);
        var sut = new AIModelRepository(ctx);

        var result = await sut.GetByProviderIdAsync(provider.Id);

        result.Should().ContainSingle(m => m.Id == model.Id);
    }

    [Fact]
    public async Task AgentRepository_GetById_IncludesModel()
    {
        await using var ctx = AgenticTestDbContextFactory.Create();
        var (_, model) = AgenticTestDbContextFactory.SeedProviderAndModel(ctx);
        var agent = AgenticTestDbContextFactory.SeedAgent(ctx, model.Id);
        AgenticTestDbContextFactory.PersistAgent(ctx, agent);
        var sut = new AgentRepository(ctx);

        var result = await sut.GetByIdAsync(agent.Id);

        result.Should().NotBeNull();
        result!.Model.Should().NotBeNull();
        result.Model!.CommercialName.Should().Be("GPT Test");
    }

    [Fact]
    public async Task AgentSessionRepository_GetActiveSession_ReturnsLatestActive()
    {
        await using var ctx = AgenticTestDbContextFactory.Create();
        var (_, model) = AgenticTestDbContextFactory.SeedProviderAndModel(ctx);
        var agent = AgenticTestDbContextFactory.SeedAgent(ctx, model.Id);
        AgenticTestDbContextFactory.PersistAgent(ctx, agent);
        var older = AgenticTestDbContextFactory.SeedSession(ctx, agent.Id);
        older.Expire();
        var active = AgenticTestDbContextFactory.SeedSession(ctx, agent.Id);
        active.RecordMessage();
        ctx.AgentSessions.AddRange(older, active);
        await ctx.SaveChangesAsync();
        var sut = new AgentSessionRepository(ctx);

        var result = await sut.GetActiveSessionAsync(agent.Id, "user-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(active.Id);
        result.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task AgentSessionRepository_GetByUserId_OrdersByLastMessage()
    {
        await using var ctx = AgenticTestDbContextFactory.Create();
        var (_, model) = AgenticTestDbContextFactory.SeedProviderAndModel(ctx);
        var agent = AgenticTestDbContextFactory.SeedAgent(ctx, model.Id);
        AgenticTestDbContextFactory.PersistAgent(ctx, agent);
        var s1 = AgenticTestDbContextFactory.SeedSession(ctx, agent.Id, "user-a");
        var s2 = AgenticTestDbContextFactory.SeedSession(ctx, agent.Id, "user-a");
        s2.RecordMessage();
        ctx.AgentSessions.AddRange(s1, s2);
        await ctx.SaveChangesAsync();
        var sut = new AgentSessionRepository(ctx);

        var result = (await sut.GetByUserIdAsync("user-a")).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(s2.Id);
    }

    [Fact]
    public async Task AgentRepository_GetAll_IncludesModel()
    {
        await using var ctx = AgenticTestDbContextFactory.Create();
        var (_, model) = AgenticTestDbContextFactory.SeedProviderAndModel(ctx);
        AgenticTestDbContextFactory.PersistAgent(ctx, AgenticTestDbContextFactory.SeedAgent(ctx, model.Id));
        var sut = new AgentRepository(ctx);

        var result = (await sut.GetAllAsync()).ToList();

        result.Should().ContainSingle(a => a.Model != null);
    }
}