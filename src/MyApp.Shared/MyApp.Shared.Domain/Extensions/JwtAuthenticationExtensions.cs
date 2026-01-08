using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring JWT Bearer authentication across microservices
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Add JWT Bearer authentication with automatic configuration from appsettings.json
    /// 
    /// Expected configuration in appsettings.json:
    /// {
    ///   "Jwt": {
    ///     "SecretKey": "your-secret-key",
    ///     "Issuer": "MyApp.Auth",
    ///     "Audience": "MyApp.Clients"
    ///   }
    /// }
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var https = jwtSettings.GetValue<bool>("RequireHttpsMetadata");

        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing. Ensure Jwt:SecretKey, Jwt:Issuer, and Jwt:Audience are set in appsettings.json");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30) // Allows 30 seconds margin for Docker/container synchronization
                };

                options.RequireHttpsMetadata = https;

                // Optional: Log authentication events for debugging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.TryAdd("X-Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
