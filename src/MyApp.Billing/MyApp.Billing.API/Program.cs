using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.OpenApi;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Infrastructure.Logging;
using MyApp.Shared.Infrastructure.OpenApi;
using Scalar.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// This line registers the DaprClient (Singleton) in the Dependency Injection (DI) container
builder.Services.AddDaprClient();

var serviceName = builder.Environment.ApplicationName ?? typeof(Program).Assembly.GetName().Name ?? "MyApp.Billing.API";

// Configure OpenTelemetry pipeline.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Configure JSON options FIRST - before AddOpenApi() so JsonSchemaExporter uses them
// builder.Services.ConfigureHttpJsonOptions(options =>
// {
//     options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//     options.SerializerOptions.Converters.Add(new MyApp.Shared.Infrastructure.Json.DateTimeConverter());
//     options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
// });

// Configure JSON options for Controllers as well
// builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
// {
//     options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
//     options.JsonSerializerOptions.Converters.Add(new MyApp.Shared.Infrastructure.Json.DateTimeConverter());
//     options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
// });

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaDocumentTransformer>();

    // Forçem el tipus a nivell d'esquema per evitar que el serialitzador intern pete
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
        {
            schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
            schema.Format = "date-time";
            schema.Default = null; // Evita que el motor intenti serialitzar un default(DateTime)
            schema.Example = null;
        }
        return Task.CompletedTask;
    });

    // També afegim el transformer compartit com a fallback
    options.AddSchemaTransformer<MyApp.Shared.Infrastructure.OpenApi.DateTimeSchemaTransformer>();
});

// Get connection string
//TODO: var billingDbConnectionString = builder.Configuration.GetConnectionString("billingdb");

// Health Checks
//TODO: builder.Services.AddCustomHealthChecks(billingDbConnectionString ?? throw new InvalidOperationException("Connection string 'billingdb' not found."));

var origins = builder.Configuration["FRONTEND_ORIGIN"]?.Split(';') ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Map OpenAPI endpoint (available for Gateway access)
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    // Use Scalar instead of SwaggerUI - lighter and more stable
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");

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
// .WithOpenApi(); // Commented out - requires Microsoft.AspNetCore.OpenApi package

// Map health check endpoint
//TODO: app.UseCustomHealthChecks();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
