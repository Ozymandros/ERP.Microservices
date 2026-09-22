namespace MyApp.Agentic.Application.AI;

/// <summary>
/// Registered agent tool.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="Verb">The verb.</param>
/// <param name="Endpoint">The endpoint.</param>
public sealed record RegisteredAgentTool(
    string Name,
    string Description,
    ToolHttpVerb Verb,
    string? Endpoint = null);

/// <summary>
/// Executes a registered agent tool by name.
/// </summary>
public interface IAgentToolExecutor
{
    /// <summary>
    /// Executes the named tool using the raw JSON arguments supplied by the model.
    /// </summary>
    /// <param name="toolName">Registered tool name.</param>
    /// <param name="arguments">Raw JSON arguments from the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool result text returned to the model.</returns>
    Task<string> ExecuteAsync(string toolName, string arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry of ERP tool metadata and execution handlers.
/// </summary>
public interface IAgentToolRegistry
{
    /// <summary>
    /// Registers a tool definition and its handler.
    /// </summary>
    /// <param name="tool">Tool metadata.</param>
    /// <param name="handler">Delegate invoked when the model calls the tool.</param>
    void RegisterTool(RegisteredAgentTool tool, Func<string, CancellationToken, Task<string>> handler);

    /// <summary>
    /// Gets the execution handler for a tool name.
    /// </summary>
    /// <param name="toolName">Registered tool name.</param>
    /// <returns>Handler delegate when found; otherwise <see langword="null"/>.</returns>
    Func<string, CancellationToken, Task<string>>? GetHandler(string toolName);

    /// <summary>
    /// Gets all registered tool definitions.
    /// </summary>
    /// <returns>Snapshot list of registered tools.</returns>
    IReadOnlyList<RegisteredAgentTool> GetRegisteredTools();
}
