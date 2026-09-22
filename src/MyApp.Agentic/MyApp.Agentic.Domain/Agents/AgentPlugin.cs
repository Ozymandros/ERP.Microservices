using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Agents;

/// <summary>
/// Agent plugin.
/// Dapr application endpoint.
/// </summary>
/// <param name="id">The id.</param>
public class AgentPlugin(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the identifier of the owning <see cref="Agent"/>.</summary>
    public Guid AgentId { get; private set; }
    /// <summary>Gets the logical name of the plugin (for example "InventoryPlugin").</summary>
    public string PluginName { get; private set; } = string.Empty;
    /// <summary>Gets the Dapr app-ID endpoint used to invoke the plugin's microservice.</summary>
    public string DaprAppIdEndpoint { get; private set; } = string.Empty;

    /// <summary>Gets the owning <see cref="Agent"/> navigation property.</summary>
    public Agent? Agent { get; private set; }

    /// <summary>
    /// Initializes a new instance of the AgentPlugin class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="agentId">The agent Id.</param>
    /// <param name="pluginName">The plugin Name.</param>
    /// <param name="daprAppIdEndpoint">The dapr App Id Endpoint.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pluginName"/> or <paramref name="daprAppIdEndpoint"/> is blank.
    /// </exception>
    public AgentPlugin(Guid id, Guid agentId, string pluginName, string daprAppIdEndpoint) : this(id)
    {
        AgentId = agentId;
        PluginName = NormalizeRequired(pluginName, nameof(pluginName));
        DaprAppIdEndpoint = NormalizeRequired(daprAppIdEndpoint, nameof(daprAppIdEndpoint));
    }

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    /// <param name="pluginName">The plugin Name.</param>
    /// <param name="daprAppIdEndpoint">The dapr App Id Endpoint.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pluginName"/> or <paramref name="daprAppIdEndpoint"/> is blank.
    /// </exception>
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