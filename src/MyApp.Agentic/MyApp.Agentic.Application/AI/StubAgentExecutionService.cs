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

        _logger.LogDebug("SystemPrompt: {SystemPrompt}", context.SystemPrompt);
        _logger.LogDebug("Temperature: {Temperature}, MaxTokens: {MaxTokens}", context.Temperature, context.MaxTokens);
        _logger.LogDebug("Context Memories: {Count}", context.ContextMemories.Count);
        _logger.LogDebug("Conversation History: {Count}", context.ConversationHistory.Count);
        _logger.LogDebug("Available Tools: {ToolCount}", context.Tools.Count);

        var model = context.Agent.Model;
        if (model is not null && string.Equals(model.Provider?.Name, "HuggingFace", StringComparison.OrdinalIgnoreCase))
        {
            // Keep execution in stub mode, but validate adapter wiring for HF-routed models.
            _ = AgentAdapterFactory.CreateHuggingFaceClient(
                model.TechnicalName,
                context.ApiKey);
            _logger.LogDebug("HuggingFace adapter initialized for model {ModelId}", model.TechnicalName);
        }

        await Task.Delay(100, cancellationToken);

        var response = $"[AI Response to: \"{userMessage}\"] " +
            $"This is a stub response. In production, this would call the AI model at {context.BaseUrl} " +
            $"with temperature ({context.Temperature}), max tokens ({context.MaxTokens}) and system prompt.";

        if (context.ContextMemories.Any())
        {
            response += $" [Relevant context: {string.Join(", ", context.ContextMemories.Take(2))}]";
        }

        if (context.Tools.Any())
        {
            response += $" [Available tools: {string.Join(", ", context.Tools.Select(t => t.Name))}]";
        }

        return response;
    }
}