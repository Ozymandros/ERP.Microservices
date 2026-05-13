using Microsoft.Extensions.Logging;
using MyApp.Agentic.Domain.Skills;
using MyApp.Agentic.Application.Contracts.Services;

namespace MyApp.Agentic.Application.Services;

public class SkillService : ISkillService
{
    private readonly Dictionary<string, SkillDefinition> _skills = new();
    private readonly ILogger<SkillService> _logger;

    public SkillService(ILogger<SkillService> logger)
    {
        _logger = logger;
    }

    public void LoadSkill(SkillDefinition skill)
    {
        _skills[skill.Name] = skill;
        _logger.LogInformation("Loaded skill: {Name} v{Version}", skill.Name, skill.Version);
    }

    public void Load(SkillDefinition skill)
    {
        _skills[skill.Name] = skill;
        _logger.LogInformation("Loaded skill: {Name} v{Version}", skill.Name, skill.Version);
    }

    public Task<string> GetSkillInstructionsAsync(string skillName, CancellationToken cancellationToken = default)
    {
        if (_skills.TryGetValue(skillName, out var skill))
        {
            return Task.FromResult(skill.Instructions);
        }

        _logger.LogWarning("Skill not found: {SkillName}", skillName);
        return Task.FromResult(string.Empty);
    }

    public Task<IEnumerable<SkillDefinition>> GetActiveSkillsAsync(CancellationToken cancellationToken = default)
    {
        var activeSkills = _skills.Values.Where(s => s.IsActive).ToList();
        return Task.FromResult<IEnumerable<SkillDefinition>>(activeSkills);
    }

    public Task<SkillDefinition?> GetSkillByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _skills.TryGetValue(name, out var skill);
        return Task.FromResult(skill);
    }

    public Task<IEnumerable<string>> GetRequiredToolsForSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        if (_skills.TryGetValue(skillName, out var skill))
        {
            return Task.FromResult<IEnumerable<string>>(skill.RequiredTools);
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    public bool IsSkillAvailable(string skillName)
    {
        return _skills.ContainsKey(skillName) && _skills[skillName].IsActive;
    }

    public IEnumerable<string> GetRegisteredSkillNames()
    {
        return _skills.Keys.ToList();
    }
}

public class AgentSkillOptions
{
    private readonly List<Action<SkillService>> _skillLoaders = new();

    public void AddSkill(string name, Action<SkillService> configure)
    {
        _skillLoaders.Add(configure);
    }

    public void LoadSkills(SkillService service)
    {
        foreach (var loader in _skillLoaders)
        {
            loader(service);
        }
    }
}