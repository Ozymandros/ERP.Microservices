using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Microsoft.IdentityModel.Tokens;

using System.Security.Claims;

using System.Text;



namespace MyApp.Shared.Infrastructure.Extensions;



/// <summary>
/// Extension methods for configuring JWT Bearer authentication across microservices.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication using Jwt:SecretKey, Jwt:Issuer, and Jwt:Audience from configuration.
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

                // Keep claim types as issued (ClaimTypes.Name, NameIdentifier URIs) for permission checks.

                options.MapInboundClaims = false;



                options.TokenValidationParameters = new TokenValidationParameters

                {

                    ValidateIssuerSigningKey = true,

                    IssuerSigningKey = key,

                    ValidateIssuer = true,

                    ValidIssuer = issuer,

                    ValidateAudience = true,

                    ValidAudience = audience,

                    ValidateLifetime = true,

                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = ClaimTypes.Name,

                    RoleClaimType = ClaimTypes.Role,

                };



                options.RequireHttpsMetadata = https;



                options.Events = new JwtBearerEvents

                {

                    OnMessageReceived = context =>

                    {

                        if (context.Request.Path.StartsWithSegments("/api/internal/permissions"))

                            context.NoResult();

                        return Task.CompletedTask;

                    },

                    OnAuthenticationFailed = context =>

                    {

                        if (context.Exception is SecurityTokenExpiredException)

                        {

                            context.Response.Headers.TryAdd("X-Token-Expired", "true");

                        }



                        var logger = context.HttpContext.RequestServices

                            .GetService<ILoggerFactory>()

                            ?.CreateLogger("JwtBearer");

                        logger?.LogWarning(

                            context.Exception,

                            "JWT authentication failed for {Path}",

                            context.Request.Path);



                        return Task.CompletedTask;

                    },

                };

            });



        services.AddAuthorization();



        return services;

    }

}


