namespace MyApp.Agentic.Domain.Agents;

/// <summary>
/// Defines the operational mode for an AI agent.
/// </summary>
public enum BotType
{
    /// <summary>
    /// Chat mode: read-only ERP tools (GET) such as get-by-name, search, and docs.
    /// POST, PUT, PATCH, and DELETE tools are not exposed.
    /// </summary>
    Chat,

    /// <summary>
    /// Agent mode: full ERP tool surface, including POST, PUT, PATCH, and DELETE operations.
    /// </summary>
    Agent
}