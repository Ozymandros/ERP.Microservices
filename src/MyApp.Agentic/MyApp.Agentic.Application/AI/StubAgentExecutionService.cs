using Microsoft.Extensions.Logging;

namespace MyApp.Agentic.Application.AI;

/// <summary>
/// Provides a non-production implementation of <see cref="IAgentExecutionService"/> that simulates
/// agent execution without calling an external AI provider.
/// </summary>
/// <remarks>
/// This service is intended for local development, testing, and integration validation where deterministic,
/// low-cost responses are preferred over real model inference.
/// </remarks>
public class StubAgentExecutionService : IAgentExecutionService
{
    private readonly ILogger<StubAgentExecutionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubAgentExecutionService"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger used to record execution metadata, debug context, and adapter initialization diagnostics.
    /// </param>
    public StubAgentExecutionService(ILogger<StubAgentExecutionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a simulated agent request and returns a deterministic stubbed response for development/testing scenarios.
    /// </summary>
    /// <param name="context">
    /// Execution metadata and runtime settings, including selected model, prompt, memories, tools, API key, and endpoint information.
    /// </param>
    /// <param name="userMessage">The end-user message to process.</param>
    /// <param name="cancellationToken">A token used to cancel the simulated execution delay.</param>
    /// <returns>
    /// A composed textual response that echoes the user message and includes selected execution details
    /// (for example model configuration, sampled memories, and available tool names).
    /// </returns>
    /// <remarks>
    /// This implementation does not call a real LLM provider.  
    /// If the configured provider is <c>HuggingFace</c>, it initializes the adapter to validate wiring only,
    /// while still keeping execution in stub mode.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is canceled during the simulated delay.
    /// </exception>
    public async Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default)
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

        return new AgentExecutionResult(response);
    }
}