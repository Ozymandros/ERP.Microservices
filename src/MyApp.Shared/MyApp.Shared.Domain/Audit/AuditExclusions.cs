namespace MyApp.Shared.Domain.Audit;

/// <summary>
/// Entity types excluded from centralized audit publishing (still persisted normally).
/// </summary>
public static class AuditExclusions
{
    // Auth — high-volume / sensitive
    /// <summary>Refresh token rows.</summary>
    public const string RefreshToken = nameof(RefreshToken);

    // Agentic — session/memory churn (separate persistence paths)
    /// <summary>Agent conversation memory rows.</summary>
    public const string AgentMemory = nameof(AgentMemory);

    /// <summary>Agent chat session rows.</summary>
    public const string AgentSession = nameof(AgentSession);

    // Audit microservice — never re-audit audit storage
    /// <summary>Audit trail header rows (MyApp.Audit.Domain.EntityChange).</summary>
    public const string EntityChange = nameof(EntityChange);

    /// <summary>Audit trail property rows (MyApp.Audit.Domain.PropertyChange).</summary>
    public const string PropertyChange = nameof(PropertyChange);

    private static readonly HashSet<string> ExcludedEntityNames =
        new(StringComparer.Ordinal)
        {
            RefreshToken,
            AgentMemory,
            AgentSession,
            EntityChange,
            PropertyChange
        };

    /// <summary>Determines whether the given entity name is excluded from audit publishing.</summary>
    /// <param name="entityName">The entity type name to check.</param>
    /// <returns><see langword="true"/> if the entity is excluded; otherwise, <see langword="false"/>.</returns>
    public static bool IsExcluded(string? entityName)
        => !string.IsNullOrWhiteSpace(entityName) && ExcludedEntityNames.Contains(entityName);

    /// <summary>Filters a collection of change snapshots by removing entries whose entity type is excluded from audit publishing.</summary>
    /// <typeparam name="T">The type of the change snapshot.</typeparam>
    /// <param name="changes">The collection of change snapshots to filter.</param>
    /// <param name="entityNameSelector">A function that extracts the entity type name from a snapshot.</param>
    /// <returns>A read-only collection containing only the non-excluded change snapshots.</returns>
    public static IReadOnlyCollection<T> FilterForAudit<T>(IReadOnlyCollection<T> changes, Func<T, string> entityNameSelector)
    {
        if (changes.Count == 0)
            return changes;

        var filtered = changes.Where(c => !IsExcluded(entityNameSelector(c))).ToList();
        return filtered.Count == changes.Count ? changes : filtered;
    }
}
