using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.AI;

public interface IAgentToolResolver
{
    IReadOnlyList<ToolDefinition> ResolveTools(Agent agent);
}

public sealed class AgentToolResolver : IAgentToolResolver
{
    private readonly IAgentToolRegistry _registry;

    public AgentToolResolver(IAgentToolRegistry registry)
    {
        _registry = registry;
    }

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
