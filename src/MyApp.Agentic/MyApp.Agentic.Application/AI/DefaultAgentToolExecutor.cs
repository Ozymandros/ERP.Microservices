namespace MyApp.Agentic.Application.AI;

public class DefaultAgentToolExecutor : IAgentToolExecutor
{
    private readonly IAgentToolRegistry _registry;

    public DefaultAgentToolExecutor(IAgentToolRegistry registry)
    {
        _registry = registry;
    }

    public async Task<string> ExecuteAsync(string toolName, string arguments, CancellationToken cancellationToken = default)
    {
        var handler = _registry.GetHandler(toolName);
        if (handler == null)
        {
            return $"Error: Tool '{toolName}' is not registered or not available in the current context.";
        }

        try
        {
            return await handler(arguments, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Error executing tool '{toolName}': {ex.Message}";
        }
    }
}
