using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// CORS: <c>AllowAnyOrigin</c> (*) in local/non-production only.
/// Production uses <c>ALLOWED_ORIGINS</c> (semicolon-separated client URLs: frontend, gateway, etc.).
/// </summary>
public static class CorsExtensions
{
    public const string AllowFrontendPolicyName = "AllowFrontend";

    /// <summary>Primary config key — any browser/client origin allowed to call the API.</summary>
    public const string AllowedOriginsKey = "ALLOWED_ORIGINS";

    /// <summary>Legacy key; still read if <see cref="AllowedOriginsKey"/> is unset.</summary>
    public const string LegacyFrontendOriginKey = "FRONTEND_ORIGIN";

    public static IServiceCollection AddAllowFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy(AllowFrontendPolicyName, policy =>
            {
                policy.AllowAnyMethod().AllowAnyHeader();

                if (environment.IsProduction())
                {
                    var origins = GetAllowedOrigins(configuration)
                        ?? throw new InvalidOperationException(
                            $"{AllowedOriginsKey} must be set in production " +
                            "(semicolon-separated URLs, e.g. https://app.example.com;https://api.example.com).");

                    policy.WithOrigins(origins)
                          .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin();
                }
            });
        });

        return services;
    }

    internal static string[]? GetAllowedOrigins(IConfiguration configuration)
    {
        var value = configuration[AllowedOriginsKey] ?? configuration[LegacyFrontendOriginKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
