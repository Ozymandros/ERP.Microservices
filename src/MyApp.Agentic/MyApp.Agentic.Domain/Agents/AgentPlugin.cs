using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Agents;

public class AgentPlugin(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid AgentId { get; private set; }
    public string PluginName { get; private set; } = string.Empty;
    public string DaprAppIdEndpoint { get; private set; } = string.Empty;

    public Agent? Agent { get; private set; }

    public AgentPlugin(Guid id, Guid agentId, string pluginName, string daprAppIdEndpoint) : this(id)
    {
        AgentId = agentId;
        PluginName = NormalizeRequired(pluginName, nameof(pluginName));
        DaprAppIdEndpoint = NormalizeRequired(daprAppIdEndpoint, nameof(daprAppIdEndpoint));
    }

    public void Update(string pluginName, string daprAppIdEndpoint)
    {
        PluginName = NormalizeRequired(pluginName, nameof(pluginName));
        DaprAppIdEndpoint = NormalizeRequired(daprAppIdEndpoint, nameof(daprAppIdEndpoint));
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}