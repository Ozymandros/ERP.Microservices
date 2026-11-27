using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.Skills;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Application.Tests;

public class SkillServiceTests
{
    private readonly SkillService _sut;

    public SkillServiceTests()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());

        _sut = new SkillService(
            unitOfWork.Object,
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<SkillService>>());
    }

    private static SkillDefinition CreateSkill(
        string name = "inventory-lookup",
        string domain = "Inventory",
        string instructions = "Look up inventory items",
        List<string>? tools = null,
        bool active = true)
    {
        var skill = new SkillDefinition(
            Guid.NewGuid(),
            name,
            domain,
            instructions,
            tools ?? new List<string> { "get_product_by_name" });

        if (!active)
            skill.Deactivate();

        return skill;
    }

    [Fact]
    public void LoadSkill_RegistersSkill()
    {
        var skill = CreateSkill();

        _sut.LoadSkill(skill);

        Assert.True(_sut.IsSkillAvailable(skill.Name));
        Assert.Contains(skill.Name, _sut.GetRegisteredSkillNames());
    }

    [Fact]
    public void Load_RegistersSkill()
    {
        var skill = CreateSkill("sales-assist");

        _sut.Load(skill);

        Assert.Contains("sales-assist", _sut.GetRegisteredSkillNames());
    }

    [Fact]
    public void LoadSkill_OverwritesExistingSkillWithSameName()
    {
        var first = CreateSkill(instructions: "v1");
        var second = CreateSkill(instructions: "v2");

        _sut.LoadSkill(first);
        _sut.LoadSkill(second);

        Assert.Single(_sut.GetRegisteredSkillNames());
    }

    [Fact]
    public async Task GetSkillInstructionsAsync_WhenFound_ReturnsInstructions()
    {
        var skill = CreateSkill(instructions: "Use inventory tools carefully");
        _sut.LoadSkill(skill);

        var result = await _sut.GetSkillInstructionsAsync(skill.Name, CancellationToken.None);

        Assert.Equal("Use inventory tools carefully", result);
    }

    [Fact]
    public async Task GetSkillInstructionsAsync_WhenMissing_ReturnsEmptyString()
    {
        var result = await _sut.GetSkillInstructionsAsync("missing-skill", CancellationToken.None);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetActiveSkillsAsync_ReturnsOnlyActiveSkills()
    {
        var active = CreateSkill("active-skill");
        var inactive = CreateSkill("inactive-skill", active: false);
        _sut.LoadSkill(active);
        _sut.LoadSkill(inactive);

        var result = (await _sut.GetActiveSkillsAsync(CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal("active-skill", result[0].Name);
    }

    [Fact]
    public async Task GetSkillByNameAsync_WhenFound_ReturnsSkill()
    {
        var skill = CreateSkill("find-me");
        _sut.LoadSkill(skill);

        var result = await _sut.GetSkillByNameAsync("find-me", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("find-me", result!.Name);
    }

    [Fact]
    public async Task GetSkillByNameAsync_WhenMissing_ReturnsNull()
    {
        var result = await _sut.GetSkillByNameAsync("nope", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRequiredToolsForSkillAsync_WhenFound_ReturnsTools()
    {
        var skill = CreateSkill(tools: new List<string> { "tool-a", "tool-b" });
        _sut.LoadSkill(skill);

        var result = (await _sut.GetRequiredToolsForSkillAsync(skill.Name, CancellationToken.None)).ToList();

        Assert.Equal(new[] { "tool-a", "tool-b" }, result);
    }

    [Fact]
    public async Task GetRequiredToolsForSkillAsync_WhenMissing_ReturnsEmpty()
    {
        var result = await _sut.GetRequiredToolsForSkillAsync("missing", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void IsSkillAvailable_WhenInactive_ReturnsFalse()
    {
        var skill = CreateSkill("paused", active: false);
        _sut.LoadSkill(skill);

        Assert.False(_sut.IsSkillAvailable("paused"));
    }

    [Fact]
    public void IsSkillAvailable_WhenMissing_ReturnsFalse()
    {
        Assert.False(_sut.IsSkillAvailable("unknown"));
    }

    [Fact]
    public void AgentSkillOptions_LoadSkills_InvokesConfiguredLoaders()
    {
        var options = new AgentSkillOptions();
        options.AddSkill("inventory", service =>
        {
            service.LoadSkill(CreateSkill("from-options"));
        });

        options.LoadSkills(_sut);

        Assert.Contains("from-options", _sut.GetRegisteredSkillNames());
    }
}
