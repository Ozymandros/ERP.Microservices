using Microsoft.AspNetCore.Mvc;
using MyApp.SemanticKernel.Services;

/// <summary>
/// REST API controller that exposes the Semantic Kernel layer to external callers.
/// Provides two endpoints: <c>POST /api/sk/invoke</c> to call a specific plugin function
/// and <c>POST /api/sk/query</c> to run a free-form natural-language prompt through the
/// configured LLM provider.
/// </summary>
[ApiController]
[Route("api/sk")]
public class SemanticController : ControllerBase
{
    private readonly SemanticKernelService _service;
    private readonly ILogger<SemanticController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SemanticController"/>.
    /// </summary>
    /// <param name="service">The orchestration service that resolves and invokes SK plugins.</param>
    /// <param name="logger">Logger for recording invocation errors.</param>
    public SemanticController(SemanticKernelService service, ILogger<SemanticController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Invokes a named plugin function and returns the serialized result.
    /// </summary>
    /// <remarks>
    /// Request body example:
    /// <code>
    /// { "skill": "Orders", "function": "Create", "input": { "customerId": "...", ... } }
    /// </code>
    /// The <c>skill</c> field must match a registered plugin class name (with or without the
    /// <c>Plugin</c> suffix). The <c>function</c> field matches the method name (with or without
    /// the <c>Async</c> suffix). The <c>input</c> object is serialized to JSON and forwarded to
    /// the plugin method.
    /// </remarks>
    /// <param name="req">The invocation request specifying the plugin, function, and input payload.</param>
    /// <returns>200 OK with the plugin result, or 400 Bad Request on failure.</returns>
    [HttpPost("invoke")]
    public async Task<IActionResult> Invoke([FromBody] InvokeRequest req)
    {
        if (req == null) return BadRequest("Request body is required.");

        try
        {
            var inputJson = req.Input?.ToString() ?? string.Empty;
            var result = await _service.InvokePluginAsync(req.Skill, req.Function, inputJson);
            return Ok(new { result = System.Text.Json.JsonDocument.Parse(result) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoke error");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Runs a free-form natural-language prompt through the configured LLM kernel and returns the response.
    /// Requires an LLM provider (e.g. DeepSeek, Azure OpenAI) to be configured via environment variables.
    /// </summary>
    /// <param name="req">The query request containing the prompt text.</param>
    /// <returns>200 OK with the LLM response string, or 400 Bad Request if no provider is configured.</returns>
    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest("Prompt is required.");

        try
        {
            var resp = await _service.RunPromptAsync(req.Prompt);
            return Ok(new { result = resp });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query error");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request body for <c>POST /api/sk/invoke</c>.
/// </summary>
/// <param name="Skill">The plugin name to invoke (e.g. <c>"Orders"</c> or <c>"OrdersPlugin"</c>).</param>
/// <param name="Function">The function to call on the plugin (e.g. <c>"Create"</c> or <c>"CreateAsync"</c>).</param>
/// <param name="Input">Optional JSON payload forwarded as the plugin function's input argument.</param>
public record InvokeRequest(string Skill, string Function, System.Text.Json.JsonElement? Input);

/// <summary>
/// Request body for <c>POST /api/sk/query</c>.
/// </summary>
/// <param name="Prompt">The natural-language prompt to send to the configured LLM provider.</param>
public record QueryRequest(string Prompt);
