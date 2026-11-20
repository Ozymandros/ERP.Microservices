namespace MyApp.Shared.Infrastructure.Security;

/// <summary>Provides configuration options for the AES-GCM secret encryption service.</summary>
public class SecretCryptoOptions
{
    /// <summary>The configuration section name used to bind these options from <c>appsettings.json</c> or environment variables.</summary>
    public const string SectionName = "SecretCrypto";

    /// <summary>Gets or sets the Base64-encoded 256-bit (32-byte) master key used for encryption and decryption.</summary>
    public string MasterKey { get; set; } = string.Empty;
}
