using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Shared.Domain.Security;
using MyApp.Shared.Infrastructure.Security;

namespace MyApp.Shared.Infrastructure.Extensions;

public static class SecurityServiceExtensions
{
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
