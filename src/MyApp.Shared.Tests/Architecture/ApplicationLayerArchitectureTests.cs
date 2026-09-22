using FluentAssertions;

namespace MyApp.Shared.Tests.Architecture;

/// <summary>
/// Guards application-layer conventions for unit-of-work and audit publishing.
/// </summary>
public class ApplicationLayerArchitectureTests
{
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
                    if (lines[i].Contains(".SaveChangesAsync(", StringComparison.Ordinal)
                        && !lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetRelativePath(srcRoot, file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "Application services must commit via AppServiceBase.SaveChangesAsync / IUnitOfWork, not repository.SaveChangesAsync. "
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
            var servicesDir = Path.Combine(dir, "Services");
            if (!Directory.Exists(servicesDir))
                continue;

            foreach (var file in Directory.GetFiles(servicesDir, "*Service.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                if (!text.Contains("class ", StringComparison.Ordinal))
                    continue;

                if (!text.Contains(": AppServiceBase", StringComparison.Ordinal)
                    && !text.Contains(": AppServiceBase<", StringComparison.Ordinal))
                {
                    violations.Add(Path.GetRelativePath(srcRoot, file));
                }
            }
        }

        violations.Should().BeEmpty(
            "Application services must inherit AppServiceBase so commits publish audit events (except AuditExclusions). "
            + string.Join(Environment.NewLine, violations));
    }
}
