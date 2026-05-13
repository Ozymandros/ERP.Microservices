using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.AI;

/// <summary>
/// Resolves the ERP tool surface available to a specific agent at runtime.
/// </summary>
public interface IAgentToolResolver
{
    /// <summary>
    /// Resolves the tool definitions that should be exposed to the model for the given agent.
    /// </summary>
    /// <param name="agent">Agent whose bot type and configured plugins determine tool visibility.</param>
    /// <returns>Ordered list of tool definitions for LLM tool calling.</returns>
    IReadOnlyList<ToolDefinition> ResolveTools(Agent agent);
}

/// <summary>
/// Default implementation that filters registered ERP tools by agent plugins and <see cref="BotType"/>.
/// </summary>
public sealed class AgentToolResolver : IAgentToolResolver
{
    private readonly IAgentToolRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentToolResolver"/> class.
    /// </summary>
    /// <param name="registry">Registry containing all ERP tool metadata and handlers.</param>
    public AgentToolResolver(IAgentToolRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> ResolveTools(Agent agent)
    {
        var registered = _registry.GetRegisteredTools().ToList();
        if (registered.Count == 0)
            return Array.Empty<ToolDefinition>();

        IEnumerable<RegisteredAgentTool> selected;
        if (agent.Plugins.Count > 0)
        {
            var allowed = agent.Plugins
                .Select(p => p.PluginName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            selected = registered.Where(tool => allowed.Contains(tool.Name));
        }
        else
        {
            selected = registered;
        }

        if (agent.BotType == BotType.Chat)
        {
            // Chat bots: read-only ERP access (GetByName, Search, Docs, etc.).
            selected = selected.Where(tool => tool.Verb == ToolHttpVerb.Get);
        }
        // Agent bots: all registered tools, including POST/PUT/DELETE mutations.

        return selected
            .OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tool => new ToolDefinition(tool.Name, tool.Endpoint ?? string.Empty, tool.Verb)
            {
                Description = tool.Description
            })
            .ToList();
    }
}
