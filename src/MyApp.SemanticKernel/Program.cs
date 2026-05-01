using Microsoft.SemanticKernel;
using MyApp.SemanticKernel.Services;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new("MyApp.SemanticKernel.API")
{
    ConfigureServiceDependencies = services =>
    {
        // Register Semantic Kernel related services and plugins
        builder.Services.AddSingleton<AuthPlugin>();
        builder.Services.AddSingleton<BillingPlugin>();
        builder.Services.AddSingleton<CrmPlugin>();
        builder.Services.AddSingleton<InventoryPlugin>();
        builder.Services.AddSingleton<OrdersPlugin>();
        builder.Services.AddSingleton<PurchasingPlugin>();
        builder.Services.AddSingleton<SalesPlugin>();
        builder.Services.AddSingleton<SemanticKernelService>();
    }
});

// Register messaging (Dapr) and service invoker
builder.Services.AddServiceInvoker();

// Register Semantic Kernel related services and plugins
builder.Services.AddSingleton<SemanticKernelService>();
builder.Services.AddSingleton<OrdersPlugin>();
builder.Services.AddSingleton<InventoryPlugin>();
builder.Services.AddSingleton<SalesPlugin>();
builder.Services.AddSingleton<PurchasingPlugin>();
builder.Services.AddSingleton<BillingPlugin>();
builder.Services.AddSingleton<CrmPlugin>();
builder.Services.AddSingleton<AuthPlugin>();

Kernel kernel = AddSemanticKernel(builder);

var app = builder.Build();

// Import plugin instances into the kernel so they become native SK skills.
ImportSKPlugins(kernel, app);

app.UseServiceDefaults();

app.Run();

/// <summary>
/// Create and configure a Microsoft.SemanticKernel.Kernel instance and register it in the application's DI container.
/// </summary>
/// <remarks>
/// Behavior:
/// - Creates a Kernel builder and adds console logging so kernel/plugin logs are visible on the host console.
/// - If the <c>DEEPSEEK_API_KEY</c> environment variable is present, attempts to wire an OpenAI-compatible
///   chat completion connector using <c>AddOpenAIChatCompletion</c>.
///   - Optional environment variables:
///       - <c>DEEPSEEK_MODEL</c> (defaults to "gpt-4o-mini")
///       - <c>DEEPSEEK_API_URL</c> (custom API base for OpenAI-compatible endpoints)
///   - If the connector overload that accepts an apiBase is not available, the implementation gracefully falls back
///     to the overload without apiBase.
/// - After building, the kernel instance is registered in DI under its runtime type and as <c>object</c>.
/// - Registers <c>IHttpClientFactory</c> to support any HTTP calls needed by the kernel or plugins.
/// </remarks>
/// <param name="builder">The WebApplicationBuilder used for DI and configuration.</param>
/// <returns>The configured <see cref="Kernel"/> instance.</returns>
static Kernel AddSemanticKernel(WebApplicationBuilder builder)
{
    // Build a Semantic Kernel and wire OpenAI connector if DEEPSEEK_API_KEY is present
    var kernelBuilder = Kernel.CreateBuilder();
    // Add console logging into the kernel builder so plugin import/logs are visible
    kernelBuilder.Services.AddLogging(lb => lb.AddConsole());

    var deepseekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
    if (!string.IsNullOrWhiteSpace(deepseekKey))
    {
        // Configure OpenAI connector using the key for an OpenAI-compatible endpoint
        // Use a sensible default model id; callers can override with environment variables if needed.
        var model = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? "gpt-4o-mini";
        var apiUrl = Environment.GetEnvironmentVariable("DEEPSEEK_API_URL");
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            // Standard OpenAI API
            kernelBuilder.AddOpenAIChatCompletion(model, deepseekKey);
        }
        else
        {
            // Custom OpenAI-compatible endpoint (e.g. DeepSeek, Azure, local LLM)
            kernelBuilder.AddOpenAIChatCompletion(model, new Uri(apiUrl), deepseekKey);
        }
    }

    var kernel = kernelBuilder.Build();
    // Register the kernel under its runtime type and as object for resolution
    builder.Services.AddSingleton(kernel.GetType(), kernel);
    builder.Services.AddSingleton(typeof(object), kernel);
    // Register Http client factory for LLM HTTP calls
    builder.Services.AddHttpClient();
    return kernel;
}

/// <summary>
/// Import Semantic Kernel plugin instances that are registered in the application's DI container
/// into the runtime <see cref="Kernel"/> so they become native Semantic Kernel skills.
/// </summary>
/// <remarks>
/// <para>
/// This method:
/// - Creates a scoped <see cref="IServiceProvider"/> from the provided <paramref name="app"/>.
/// - Resolves the kernel instance from DI (the kernel is registered by <c>AddSemanticKernel</c>).
/// - For each known plugin type, attempts to resolve the plugin from DI. If the plugin exists,
///   it invokes the kernel's <c>ImportSkill(object, string)</c> method via reflection to register
///   the plugin under the provided skill name.
/// </para>
/// <para>
/// Implementation notes:
/// - Reflection is used to call <c>ImportSkill</c> to avoid a compile-time dependency on a
///   specific kernel API surface (provides compatibility across SK versions).
/// - Any exceptions thrown while importing a single plugin are caught and logged as warnings;
///   the import process continues with the remaining plugins. This ensures a faulty plugin
///   does not prevent the application from starting.
/// - The method is intended to be called at startup after the DI container and kernel have been built.
/// </para>
/// </remarks>
/// <param name="kernel">The <see cref="Kernel"/> instance created by <c>AddSemanticKernel</c>.</param>
/// <param name="app">The <see cref="WebApplication"/> used to create a service scope for resolving plugins and loggers.</param>
static void ImportSKPlugins(Kernel kernel, WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        var kernelInstance = sp.GetService(kernel.GetType());
        if (kernelInstance is not null)
        {
            // Import each registered plugin if available in DI using the typed kernel API
            void ImportIfExists<TPlugin>(IServiceProvider svc, object k, string skillName)
            {
                var plugin = svc.GetService(typeof(TPlugin));
                if (plugin is not null)
                {
                    try
                    {
                        var importMethod = k.GetType().GetMethod("ImportSkill", new[] { typeof(object), typeof(string) });
                        importMethod?.Invoke(k, new object[] { plugin!, skillName });
                    }
                    catch (Exception ex)
                    {
                        var logger = svc.GetService<ILogger<Program>>();
                        logger?.LogWarning(ex, "Failed to import plugin {Plugin} as skill {Skill}", typeof(TPlugin).Name, skillName);
                    }
                }
            }

            ImportIfExists<OrdersPlugin>(sp, kernelInstance, "Orders");
            ImportIfExists<InventoryPlugin>(sp, kernelInstance, "Inventory");
            ImportIfExists<SalesPlugin>(sp, kernelInstance, "Sales");
            ImportIfExists<PurchasingPlugin>(sp, kernelInstance, "Purchasing");
            ImportIfExists<BillingPlugin>(sp, kernelInstance, "Billing");
            ImportIfExists<CrmPlugin>(sp, kernelInstance, "Crm");
            ImportIfExists<AuthPlugin>(sp, kernelInstance, "Auth");
        }
    }
}