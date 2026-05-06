namespace MyApp.Agentic.Domain.Agents;

/// <summary>
/// Defines the operational mode for an AI agent.
/// </summary>
public enum BotType
{
    /// <summary>Basic chat mode - conversational only, no tool execution.</summary>
    Chat,
    /// <summary>Full agent mode - supports tool/plugin execution via Dapr service invocation.</summary>
    Agent
}