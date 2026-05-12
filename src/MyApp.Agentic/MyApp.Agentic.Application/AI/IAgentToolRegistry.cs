namespace MyApp.Agentic.Application.AI;

public interface IAgentToolExecutor
{
    Task<string> ExecuteAsync(string toolName, string arguments, CancellationToken cancellationToken = default);
}

public interface IAgentToolRegistry
{
    void RegisterTool(string toolName, Func<string, CancellationToken, Task<string>> handler);
    Func<string, CancellationToken, Task<string>>? GetHandler(string toolName);
    IEnumerable<string> GetRegisteredToolNames();
}
