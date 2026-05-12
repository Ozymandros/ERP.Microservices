using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.Contracts.DTOs;
using System.Text.Json;

namespace MyApp.Agentic.Application.AI;

public class MicrosoftAgentExecutionService : IAgentExecutionService
{
    private readonly IAgentRuntimeFactory _runtimeFactory;
    private readonly IAgentToolExecutor _toolExecutor;
    private readonly ILogger<MicrosoftAgentExecutionService> _logger;

    public MicrosoftAgentExecutionService(
        IAgentRuntimeFactory runtimeFactory,
        IAgentToolExecutor toolExecutor,
        ILogger<MicrosoftAgentExecutionService> logger)
    {
        _runtimeFactory = runtimeFactory;
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing real AI request for Agent {AgentId} with Model {ModelName} via MAF",
            context.Agent.Id, context.Agent.Model?.TechnicalName ?? "Unknown");

        var client = _runtimeFactory.CreateClient(context);
        
        var messages = BuildMessages(context, userMessage);
        var tools = BuildTools(context);

        var options = new ChatOptions
        {
            Temperature = (float)context.Temperature,
            MaxOutputTokens = context.MaxTokens,
            Tools = tools,
            ToolMode = tools.Any() ? ChatToolMode.Auto : ChatToolMode.None
        };

        var toolCallResults = new List<ToolCallResult>();
        
        // Simple execution loop for tool calling
        // Note: IChatClient can handle this automatically if we use a middleware, 
        // but for explicit control and logging we'll do a basic loop here.
        
        int iterations = 0;
        const int maxIterations = 5;
        string? finalContent = null;
        string? finishReason = null;

        while (iterations < maxIterations)
        {
            iterations++;
            var response = await client.GetResponseAsync(messages, options, cancellationToken);
            
            var assistantMessage = response.Messages[0];
            messages.Add(assistantMessage);

            if (assistantMessage.Contents.OfType<TextContent>().Any())
            {
                finalContent = string.Join("\n", assistantMessage.Contents.OfType<TextContent>().Select(t => t.Text));
            }

            var toolCalls = assistantMessage.Contents.OfType<FunctionCallContent>().ToList();
            if (!toolCalls.Any())
            {
                finishReason = response.FinishReason?.ToString();
                break;
            }

            foreach (var call in toolCalls)
            {
                _logger.LogInformation("AI requested tool call: {ToolName} with args {Args}", call.Name, call.Arguments);
                
                var argsJson = call.Arguments != null ? JsonSerializer.Serialize(call.Arguments) : "{}";
                var result = await _toolExecutor.ExecuteAsync(call.Name, argsJson, cancellationToken);
                
                toolCallResults.Add(new ToolCallResult(
                    call.Name,
                    argsJson,
                    result,
                    !result.StartsWith("Error:")));

                messages.Add(new ChatMessage(ChatRole.Tool, result)
                {
                    Contents = { new FunctionResultContent(call.CallId, result) }
                });
            }
        }

        if (iterations >= maxIterations)
        {
            _logger.LogWarning("Reached max tool iterations ({Max}) for Agent {AgentId}", maxIterations, context.Agent.Id);
            finishReason = "MaxIterationsReached";
        }

        return new AgentExecutionResult(
            finalContent ?? string.Empty,
            toolCallResults,
            finishReason);
    }

    private static List<ChatMessage> BuildMessages(AgentExecutionContext context, string userMessage)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(context.SystemPrompt))
        {
            messages.Add(new ChatMessage(ChatRole.System, context.SystemPrompt));
        }

        // Add history
        foreach (var historyItem in context.ConversationHistory)
        {
            // Simple parsing of "Role: Content" format used in AgentService
            var parts = historyItem.Split(':', 2);
            if (parts.Length == 2)
            {
                var role = parts[0].Trim().ToLower() switch
                {
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    _ => ChatRole.User
                };
                messages.Add(new ChatMessage(role, parts[1].Trim()));
            }
        }

        // Add RAG context as a system message or part of the user message
        if (context.ContextMemories.Any())
        {
            var contextText = "Relevant context information:\n" + string.Join("\n", context.ContextMemories);
            messages.Add(new ChatMessage(ChatRole.System, contextText));
        }

        messages.Add(new ChatMessage(ChatRole.User, userMessage));

        return messages;
    }

    private List<AITool> BuildTools(AgentExecutionContext context)
    {
        var tools = new List<AITool>();
        foreach (var toolDef in context.Tools)
        {
            // We create a function that just returns its name and args, 
            // the loop will handle the actual execution via _toolExecutor.
            // This allows us to keep the IAgentToolExecutor abstraction.
            
            var function = AIFunctionFactory.Create((string arguments) => "Handled by loop", toolDef.Name, $"Execute {toolDef.Name} operation");
            tools.Add(function);
        }
        return tools;
    }
}
