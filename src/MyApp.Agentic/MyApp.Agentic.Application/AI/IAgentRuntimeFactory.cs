using Microsoft.Extensions.AI;

namespace MyApp.Agentic.Application.AI;

public interface IAgentRuntimeFactory
{
    IChatClient CreateClient(AgentExecutionContext context);
}
