using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace MyApp.Shared.Infrastructure.Extensions;

public static class HealthChecksExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, string dbConnectionString)
    {
        services.AddHealthChecks()
            .AddSqlServer(dbConnectionString, name: "SQL Server", tags: new[] { "db", "sql", "sqlserver" })
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "api" });

        return services;
    }

    public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    Status = report.Status.ToString(),
                    Duration = report.TotalDuration,
                    Components = report.Entries.Select(e => new
                    {
                        Component = e.Key,
                        Status = e.Value.Status.ToString(),
                        Description = e.Value.Description,
                        Duration = e.Value.Duration
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
        });

        return app;
    }
}
