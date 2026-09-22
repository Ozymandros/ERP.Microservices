using MyApp.Shared.Domain.Repositories;

namespace MyApp.Agentic.Domain.Skills;

/// <summary>
/// Repository contract for <see cref="SkillDefinition"/> persistence and retrieval.
/// </summary>
public interface ISkillRepository : IRepository<SkillDefinition, Guid>
{
    /// <summary>
    /// Retrieves a skill definition by its canonical name.
    /// </summary>
    /// <param name="name">Canonical skill name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching skill, or <see langword="null"/> if not found.</returns>
    Task<SkillDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently active skill definitions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active skill definitions.</returns>
    Task<IEnumerable<SkillDefinition>> GetActiveSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the instructions text for a skill by name.
    /// </summary>
    /// <param name="name">Canonical skill name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The skill instructions, or an empty string if not found.</returns>
    Task<string> GetSkillInstructionsAsync(string name, CancellationToken cancellationToken = default);
}