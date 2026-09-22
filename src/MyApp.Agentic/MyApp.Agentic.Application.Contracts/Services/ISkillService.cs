using MyApp.Agentic.Domain.Skills;

namespace MyApp.Agentic.Application.Contracts.Services;

/// <summary>Service contract for querying and managing agent skill definitions.</summary>
public interface ISkillService
{
    /// <summary>Gets the system-prompt instructions for the named skill.</summary>
    /// <param name="skillName">Canonical skill name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instructions text, or an empty string if the skill is not found.</returns>
    Task<string> GetSkillInstructionsAsync(string skillName, CancellationToken cancellationToken = default);

    /// <summary>Returns all currently active skill definitions.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active skill definitions.</returns>
    Task<IEnumerable<SkillDefinition>> GetActiveSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a skill definition by its canonical name.</summary>
    /// <param name="name">Canonical skill name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching skill, or <see langword="null"/> if not found.</returns>
    Task<SkillDefinition?> GetSkillByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets the list of required tool names for the named skill.</summary>
    /// <param name="skillName">Canonical skill name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Required tool names, or an empty enumerable if the skill is not found.</returns>
    Task<IEnumerable<string>> GetRequiredToolsForSkillAsync(string skillName, CancellationToken cancellationToken = default);

    /// <summary>Indicates whether a skill with the given name is registered and available.</summary>
    /// <param name="skillName">Canonical skill name.</param>
    /// <returns><see langword="true"/> if the skill is available; otherwise <see langword="false"/>.</returns>
    bool IsSkillAvailable(string skillName);

    /// <summary>Returns the names of all registered skills.</summary>
    /// <returns>Registered skill names.</returns>
    IEnumerable<string> GetRegisteredSkillNames();
}