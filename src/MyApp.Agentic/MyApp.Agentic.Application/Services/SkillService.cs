using Microsoft.Extensions.Logging;
using MyApp.Agentic.Domain.Skills;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Application.Services;

public class SkillService : AppServiceBase, ISkillService
{
    private readonly Dictionary<string, SkillDefinition> _skills = new();
    private readonly ILogger<SkillService> _logger;

    /// <summary>
    /// Initializes a new instance of the SkillService class.
    /// </summary>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public SkillService(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<SkillService> logger)
        : base(unitOfWork, eventPublisher, logger, ServiceNames.Agentic)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads the skill.
    /// </summary>
    /// <param name="skill">The skill.</param>
    public void LoadSkill(SkillDefinition skill)
    {
        _skills[skill.Name] = skill;
        _logger.LogInformation("Loaded skill: {Name} v{Version}", skill.Name, skill.Version);
    }

    /// <summary>
    /// Load.
    /// </summary>
    /// <param name="skill">The skill.</param>
    public void Load(SkillDefinition skill)
    {
        _skills[skill.Name] = skill;
        _logger.LogInformation("Loaded skill: {Name} v{Version}", skill.Name, skill.Version);
    }

    /// <summary>
    /// Gets the skill instructions asynchronously.
    /// </summary>
    /// <param name="skillName">The skill Name.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public Task<string> GetSkillInstructionsAsync(string skillName, CancellationToken cancellationToken = default)
    {
        if (_skills.TryGetValue(skillName, out var skill))
        {
            return Task.FromResult(skill.Instructions);
        }

        _logger.LogWarning("Skill not found: {SkillName}", skillName);
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Gets the active skills asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public Task<IEnumerable<SkillDefinition>> GetActiveSkillsAsync(CancellationToken cancellationToken = default)
    {
        var activeSkills = _skills.Values.Where(s => s.IsActive).ToList();
        return Task.FromResult<IEnumerable<SkillDefinition>>(activeSkills);
    }

    /// <summary>
    /// Gets the skill by name asynchronously.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result if found; otherwise, <c>null</c>.</returns>
    public Task<SkillDefinition?> GetSkillByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _skills.TryGetValue(name, out var skill);
        return Task.FromResult(skill);
    }

    /// <summary>
    /// Gets the required tools for skill asynchronously.
    /// </summary>
    /// <param name="skillName">The skill Name.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    public Task<IEnumerable<string>> GetRequiredToolsForSkillAsync(string skillName, CancellationToken cancellationToken = default)
    {
        if (_skills.TryGetValue(skillName, out var skill))
        {
            return Task.FromResult<IEnumerable<string>>(skill.RequiredTools);
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    /// <summary>
    /// Determines whether skill available.
    /// </summary>
    /// <param name="skillName">The skill Name.</param>
    /// <returns><c>true</c> if the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsSkillAvailable(string skillName)
    {
        return _skills.ContainsKey(skillName) && _skills[skillName].IsActive;
    }

    /// <summary>
    /// Gets the registered skill names.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<string> GetRegisteredSkillNames()
    {
        return _skills.Keys.ToList();
    }
}

public class AgentSkillOptions
{
    private readonly List<Action<SkillService>> _skillLoaders = new();

    /// <summary>
    /// Adds a skill.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="configure">The configure.</param>
    public void AddSkill(string name, Action<SkillService> configure)
    {
        _skillLoaders.Add(configure);
    }

    /// <summary>
    /// Loads the skills.
    /// </summary>
    /// <param name="service">The service.</param>
    public void LoadSkills(SkillService service)
    {
        foreach (var loader in _skillLoaders)
        {
            loader(service);
        }
    }
}