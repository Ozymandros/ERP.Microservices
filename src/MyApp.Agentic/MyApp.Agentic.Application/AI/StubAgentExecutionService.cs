using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Application.AI;

public class StubAgentExecutionService : IAgentExecutionService
{
    private readonly ILogger<StubAgentExecutionService> _logger;

    public StubAgentExecutionService(ILogger<StubAgentExecutionService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing AI request for Agent {AgentId} with Model {ModelName}",
            context.Agent.Id, context.Agent.Model?.TechnicalName ?? "Unknown");

        _logger.LogDebug("System Instructions: {Instructions}", context.Agent.SystemInstructions);
        _logger.LogDebug("Temperature: {Temperature}, MaxTokens: {MaxTokens}", context.Temperature, context.MaxTokens);
        _logger.LogDebug("Context Memories: {Count}", context.ContextMemories.Count);
        _logger.LogDebug("Conversation History: {Count}", context.ConversationHistory.Count);

        await Task.Delay(100, cancellationToken);

        var response = $"[AI Response to: \"{userMessage}\"] " +
            $"This is a stub response. In production, this would call the AI model at {context.BaseUrl} " +
            $"with temperature ({context.Temperature}), max tokens ({context.MaxTokens}) and system instructions.";

        if (context.ContextMemories.Any())
        {
            response += $" [Relevant context: {string.Join(", ", context.ContextMemories.Take(2))}]";
        }

        return response;
    }
}