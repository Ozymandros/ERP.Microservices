namespace MyApp.Agentic.Application.AI;

/// <summary>
/// In-memory registry of ERP tool metadata and execution handlers.
/// </summary>
public class AgentToolRegistry : IAgentToolRegistry
{
    private readonly Dictionary<string, RegisteredAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<string, CancellationToken, Task<string>>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void RegisterTool(RegisteredAgentTool tool, Func<string, CancellationToken, Task<string>> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tool.Name);
        _tools[tool.Name] = tool;
        _handlers[tool.Name] = handler;
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public Func<string, CancellationToken, Task<string>>? GetHandler(string toolName)
    {
        return _handlers.TryGetValue(toolName, out var handler) ? handler : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<RegisteredAgentTool> GetRegisteredTools() => _tools.Values.ToList();
}
