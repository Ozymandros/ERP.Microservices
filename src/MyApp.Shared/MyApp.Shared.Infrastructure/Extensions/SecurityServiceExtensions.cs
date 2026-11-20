using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared.Domain.Security;
using MyApp.Shared.Infrastructure.Security;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>Provides extension methods for registering security services such as log sanitization and secret encryption.</summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// Adds a log sanitizer.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLogSanitizer(this IServiceCollection services)
    {
        services.AddSingleton<ILogSanitizer, LogSanitizer>();
        return services;
    }

    /// <summary>
    /// Adds a secret crypto.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section Name.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecretCrypto(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SecretCryptoOptions.SectionName)
    {
        services
            .AddOptions<SecretCryptoOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(
                options =>
                {
                    if (string.IsNullOrWhiteSpace(options.MasterKey))
                        return false;

                    try
                    {
                        var bytes = Convert.FromBase64String(options.MasterKey.Trim());
                        return bytes.Length == 32;
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                },
                $"{sectionName}:MasterKey must be a valid Base64-encoded 32-byte key.")
            .ValidateOnStart();

        services.AddSingleton<ISecretCryptoService, AesGcmSecretCryptoService>();
        return services;
    }
}
