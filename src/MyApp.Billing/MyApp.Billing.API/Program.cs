using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Service Defaults Configuration
// ============================================================================
builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Billing.API",
    EnableHealthChecks = false, // TODO: Enable when database is configured
    EnableRedisCache = false, // TODO: Enable when needed
    EnableAutoMapper = false, // No domain logic yet
    DbContextType = null, // TODO: Add when billing database is implemented
    // ConnectionStringKey will be null (no database yet)
    ConfigureServiceDependencies = services =>
    {
        // TODO: Add billing-specific repositories and services here
        // services.AddScoped<IBillingRepository, BillingRepository>();
        // services.AddScoped<IBillingService, BillingService>();
    }
});

var app = builder.Build();

// ============================================================================
// Service Defaults Pipeline
// ============================================================================
// Options are automatically reused from AddServiceDefaults via DI.
app.UseServiceDefaults();

// ============================================================================
// Temporary: Weather forecast endpoint (TODO: Remove when billing endpoints added)
// ============================================================================
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
