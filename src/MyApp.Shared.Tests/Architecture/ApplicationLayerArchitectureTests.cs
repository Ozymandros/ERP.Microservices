using System.Text.RegularExpressions;
using FluentAssertions;

namespace MyApp.Shared.Tests.Architecture;

/// <summary>
/// Guards application-layer conventions for unit-of-work and audit publishing
/// after the hexagonal rollout (use cases + transaction boundaries).
/// </summary>
public class ApplicationLayerArchitectureTests
{
    /// <summary>
    /// Allowed commit receivers: AppServiceBase path via boundary/base/UoW — not repositories or DbContext.
    /// </summary>
    private static readonly Regex AllowedSaveChanges = new(
        @"\b(boundary|base|_?unitOfWork|UnitOfWork)\.SaveChangesAsync\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AnySaveChanges = new(
        @"\.SaveChangesAsync\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void ApplicationLayer_DoesNotCallRepositorySaveChangesAsync()
    {
        var srcRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var applicationDirs = Directory.GetDirectories(srcRoot, "*.Application", SearchOption.AllDirectories)
            .Where(d => !d.Contains("test", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var violations = new List<string>();

        foreach (var dir in applicationDirs)
        {
            foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                        continue;

                    if (!AnySaveChanges.IsMatch(line))
                        continue;

                    if (AllowedSaveChanges.IsMatch(line))
                        continue;

                    violations.Add($"{Path.GetRelativePath(srcRoot, file)}:{i + 1}: {line.Trim()}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Application code must commit via transaction boundary / AppServiceBase / IUnitOfWork, "
            + "not repository or DbContext SaveChangesAsync. "
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationServices_InheritAppServiceBase_ForAuditCommitPath()
    {
        var srcRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var applicationDirs = Directory.GetDirectories(srcRoot, "*.Application", SearchOption.AllDirectories)
            .Where(d => !d.Contains("test", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var violations = new List<string>();

        foreach (var dir in applicationDirs)
        {
            // Hexagonal: thin *Service façades may not inherit AppServiceBase; write commits go through
            // *TransactionBoundary : AppServiceBase. Services that still call SaveChangesAsync themselves
            // must inherit AppServiceBase.
            var servicesDir = Path.Combine(dir, "Services");
            if (Directory.Exists(servicesDir))
            {
                foreach (var file in Directory.GetFiles(servicesDir, "*Service.cs", SearchOption.AllDirectories))
                {
                    var text = File.ReadAllText(file);
                    if (!text.Contains("class ", StringComparison.Ordinal))
                        continue;

                    var commitsDirectly = AnySaveChanges.IsMatch(text);
                    var inheritsAppServiceBase =
                        text.Contains(": AppServiceBase", StringComparison.Ordinal)
                        || text.Contains(": AppServiceBase<", StringComparison.Ordinal);

                    if (commitsDirectly && !inheritsAppServiceBase)
                        violations.Add(Path.GetRelativePath(srcRoot, file));
                }
            }

            var internalsDir = Path.Combine(dir, "Internals");
            if (!Directory.Exists(internalsDir))
                continue;

            foreach (var file in Directory.GetFiles(internalsDir, "*TransactionBoundary.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                if (!text.Contains("class ", StringComparison.Ordinal)
                    && !text.Contains("record ", StringComparison.Ordinal))
                    continue;

                var inheritsAppServiceBase =
                    text.Contains(": AppServiceBase", StringComparison.Ordinal)
                    || text.Contains(": AppServiceBase<", StringComparison.Ordinal)
                    || text.Contains("AppServiceBase(", StringComparison.Ordinal);

                if (!inheritsAppServiceBase)
                    violations.Add(Path.GetRelativePath(srcRoot, file));
            }
        }

        violations.Should().BeEmpty(
            "Write commits must go through AppServiceBase (service or *TransactionBoundary) so audit events publish. "
            + string.Join(Environment.NewLine, violations));
    }
}
