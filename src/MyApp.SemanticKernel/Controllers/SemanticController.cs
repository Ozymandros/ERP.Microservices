using Microsoft.AspNetCore.Mvc;
using MyApp.SemanticKernel.Services;

[ApiController]
[Route("api/sk")]
public class SemanticController : ControllerBase
{
    private readonly SemanticKernelService _service;
    private readonly ILogger<SemanticController> _logger;

    public SemanticController(SemanticKernelService service, ILogger<SemanticController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Invoke a plugin function: POST /api/sk/invoke
    /// Body: { "skill": "Orders", "function": "Create", "input": { ... } }
    /// </summary>
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
    /// Run a prompt through LLM Kernel (requires LLM provider configured)
    /// </summary>
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

public record InvokeRequest(string Skill, string Function, System.Text.Json.JsonElement? Input);
public record QueryRequest(string Prompt);
