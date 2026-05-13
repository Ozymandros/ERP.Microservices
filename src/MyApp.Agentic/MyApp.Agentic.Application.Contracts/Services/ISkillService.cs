using MyApp.Agentic.Domain.Skills;

namespace MyApp.Agentic.Application.Contracts.Services;

public interface ISkillService
{
    Task<string> GetSkillInstructionsAsync(string skillName, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillDefinition>> GetActiveSkillsAsync(CancellationToken cancellationToken = default);
    Task<SkillDefinition?> GetSkillByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRequiredToolsForSkillAsync(string skillName, CancellationToken cancellationToken = default);
    bool IsSkillAvailable(string skillName);
    IEnumerable<string> GetRegisteredSkillNames();
}