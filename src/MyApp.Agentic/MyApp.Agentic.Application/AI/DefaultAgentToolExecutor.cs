namespace MyApp.Agentic.Application.AI;

/// <summary>
/// Default registry-backed implementation of <see cref="IAgentToolExecutor"/>.
/// </summary>
public class DefaultAgentToolExecutor : IAgentToolExecutor
{
    private readonly IAgentToolRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAgentToolExecutor"/> class.
    /// </summary>
    /// <param name="registry">Registry containing registered ERP tool handlers.</param>
    public DefaultAgentToolExecutor(IAgentToolRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
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
