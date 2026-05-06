using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.Skills;

public interface ISkillRepository : IRepository<SkillDefinition, Guid>
{
    Task<SkillDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillDefinition>> GetActiveSkillsAsync(CancellationToken cancellationToken = default);
    Task<string> GetSkillInstructionsAsync(string name, CancellationToken cancellationToken = default);
}