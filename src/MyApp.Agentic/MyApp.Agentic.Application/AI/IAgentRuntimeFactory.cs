using Microsoft.Extensions.AI;

namespace MyApp.Agentic.Application.AI;

/// <summary>Factory contract for creating provider-specific <see cref="IChatClient"/> instances.</summary>
public interface IAgentRuntimeFactory
{
    /// <summary>Creates a <see cref="IChatClient"/> appropriate for the provider specified in the execution context.</summary>
    /// <param name="context">Execution context containing the agent, API key, and base URL.</param>
    /// <returns>Configured chat client.</returns>
    IChatClient CreateClient(AgentExecutionContext context);
}
