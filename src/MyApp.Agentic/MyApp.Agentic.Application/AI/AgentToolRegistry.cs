namespace MyApp.Agentic.Application.AI;

public class AgentToolRegistry : IAgentToolRegistry
{
    private readonly Dictionary<string, Func<string, CancellationToken, Task<string>>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterTool(string toolName, Func<string, CancellationToken, Task<string>> handler)
    {
        _handlers[toolName] = handler;
    }

    public Func<string, CancellationToken, Task<string>>? GetHandler(string toolName)
    {
        return _handlers.TryGetValue(toolName, out var handler) ? handler : null;
    }

    public IEnumerable<string> GetRegisteredToolNames() => _handlers.Keys;
}
