using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.SemanticKernel.Services;

public class SemanticKernelService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly object? _kernelObj;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SemanticKernelService(IServiceProvider provider, ILogger<SemanticKernelService> logger)
    {
        _provider = provider;
        _logger = logger;
        // Try to resolve the concrete Kernel instance registered in Program.cs (registered under its runtime type).
        object? kernelObj = null;
        try
        {
            var kernelType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .FirstOrDefault(t => t.Name == "Kernel" || t.FullName == "Microsoft.SemanticKernel.Kernel");
            if (kernelType != null)
            {
                kernelObj = provider.GetService(kernelType);
            }
        }
        catch { kernelObj = null; }
        _kernelObj = kernelObj;
        _httpClientFactory = provider.GetService<IHttpClientFactory>() ?? throw new InvalidOperationException("IHttpClientFactory not registered");
        _configuration = provider.GetService<IConfiguration>() ?? throw new InvalidOperationException("IConfiguration not available");
    }

    public async Task<string> InvokePluginAsync(string skill, string function, string inputJson)
    {
        if (string.IsNullOrWhiteSpace(skill)) throw new ArgumentException("skill required", nameof(skill));
        if (string.IsNullOrWhiteSpace(function)) throw new ArgumentException("function required", nameof(function));

        // If kernel is available, try invoking the function via the kernel first so we get
        // native SK behavior (planning, chaining, semantic function wiring).
        if (_kernelObj is not null)
        {
            try
            {
                var fullName = skill.EndsWith("Plugin", StringComparison.OrdinalIgnoreCase)
                    ? skill.Substring(0, skill.Length - "Plugin".Length)
                    : skill;
                var funcName = fullName + "." + function;
                // Try to find InvokeAsync/RunAsync method on the kernel instance via reflection
                var kernelType = _kernelObj.GetType();
                var candidates = kernelType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => string.Equals(m.Name, "InvokeAsync", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, "RunAsync", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, "Run", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var m in candidates)
                {
                    var ps = m.GetParameters();
                    object? invokeResult = null;
                    try
                    {
                        if (ps.Length == 1)
                        {
                            // Try calling with the function name only
                            invokeResult = m.Invoke(_kernelObj, new object[] { funcName });
                        }
                        else if (ps.Length == 2)
                        {
                            // Try (string function, string input)
                            invokeResult = m.Invoke(_kernelObj, new object[] { funcName, inputJson });
                        }
                        else if (ps.Length == 0)
                        {
                            invokeResult = m.Invoke(_kernelObj, null);
                        }
                    }
                    catch
                    {
                        // ignore and try next candidate
                        continue;
                    }

                    if (invokeResult is System.Threading.Tasks.Task invokeTask)
                    {
                        await invokeTask.ConfigureAwait(false);
                        var resultProp = invokeTask.GetType().GetProperty("Result");
                        var val = resultProp != null ? resultProp.GetValue(invokeTask) : null;
                        if (val != null)
                            return System.Text.Json.JsonSerializer.Serialize(val);
                        return string.Empty;
                    }

                    if (invokeResult != null)
                        return System.Text.Json.JsonSerializer.Serialize(invokeResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Kernel invocation failed, falling back to reflection invoker");
            }
        }

        var pluginTypeName = skill.EndsWith("Plugin", StringComparison.OrdinalIgnoreCase)
            ? skill
            : skill + "Plugin";

        // Find plugin type in loaded assemblies
        var pluginType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .FirstOrDefault(t => string.Equals(t.Name, pluginTypeName, StringComparison.OrdinalIgnoreCase));

        if (pluginType == null)
            throw new InvalidOperationException($"Plugin type '{pluginTypeName}' not found.");

        // Get instance from DI or create
        var pluginInstance = _provider.GetService(pluginType) ?? ActivatorUtilities.CreateInstance(_provider, pluginType);

        // Resolve method: try several name variants
        var candidateNames = new[] { function, function + "Async", function + "Async" }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Common aliases
        if (string.Equals(function, "Create", StringComparison.OrdinalIgnoreCase))
            candidateNames.AddRange(new[] { "CreateAsync", "CreateOrderAsync", "CreateAsync" });
        if (string.Equals(function, "Get", StringComparison.OrdinalIgnoreCase) || string.Equals(function, "GetById", StringComparison.OrdinalIgnoreCase))
            candidateNames.AddRange(new[] { "GetByIdAsync", "GetAsync", "GetById" });
        if (string.Equals(function, "Update", StringComparison.OrdinalIgnoreCase))
            candidateNames.AddRange(new[] { "UpdateAsync", "UpdateOrderAsync" });
        if (string.Equals(function, "Delete", StringComparison.OrdinalIgnoreCase))
            candidateNames.AddRange(new[] { "DeleteAsync", "DeleteOrderAsync" });

        MethodInfo? method = null;
        foreach (var name in candidateNames)
        {
            method = pluginType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (method != null) break;
        }

        if (method == null)
            throw new InvalidOperationException($"Function '{function}' not found on plugin '{pluginTypeName}'.");

        // Prepare parameters: if method expects single string param, pass inputJson. If no params, ignore.
        var parameters = method.GetParameters();
        object?[] args;
        if (parameters.Length == 0)
        {
            args = Array.Empty<object?>();
        }
        else if (parameters.Length == 1)
        {
            var pType = parameters[0].ParameterType;
            if (pType == typeof(string))
            {
                args = new object?[] { inputJson };
            }
            else
            {
                // Try to deserialize inputJson into parameter type
                var deserialized = System.Text.Json.JsonSerializer.Deserialize(inputJson ?? "{}", pType, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                args = new object?[] { deserialized };
            }
        }
        else
        {
            throw new InvalidOperationException("Plugin methods with more than one parameter are not supported by the generic invoker.");
        }

        // Invoke
        var resultObj = method.Invoke(pluginInstance, args);

        if (resultObj is System.Threading.Tasks.Task task)
        {
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            var value = resultProperty != null ? resultProperty.GetValue(task) : null;
            return value != null ? System.Text.Json.JsonSerializer.Serialize(value) : string.Empty;
        }

        return resultObj != null ? System.Text.Json.JsonSerializer.Serialize(resultObj) : string.Empty;
    }

    public async Task<string> RunPromptAsync(string prompt)
    {
        // If kernel with AI services is available, use it. Otherwise, try a simple HTTP call to OpenAI-compatible API
        // If kernel is available and has a chat completion service, use kernel to run prompt
        if (_kernelObj is not null)
        {
            try
            {
                var kernelType = _kernelObj.GetType();
                var runMethod = kernelType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name.Equals("RunAsync", StringComparison.OrdinalIgnoreCase) || m.Name.Equals("InvokeAsync", StringComparison.OrdinalIgnoreCase));
                if (runMethod != null)
                {
                    // Try RunAsync(prompt) or InvokeAsync(prompt)
                    object? runResult = null;
                    try
                    {
                        runResult = runMethod.Invoke(_kernelObj, new object[] { prompt });
                    }
                    catch
                    {
                        // ignore - try other overloads
                    }

                    if (runResult is System.Threading.Tasks.Task runTask)
                    {
                        await runTask.ConfigureAwait(false);
                        var resultProp = runTask.GetType().GetProperty("Result");
                        var val = resultProp != null ? resultProp.GetValue(runTask) : null;
                        if (val != null)
                            return System.Text.Json.JsonSerializer.Serialize(val);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kernel RunAsync failed, no fallback available.");
                throw new InvalidOperationException("Kernel RunAsync failed", ex);
            }
        }

        _logger.LogWarning("No kernel with AI provider configured and HTTP fallback was removed. Set DEEPSEEK_API_KEY and ensure the Kernel is configured.");
        throw new InvalidOperationException("No LLM provider configured. Set DEEPSEEK_API_KEY and restart the service.");
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.GetTypes();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
