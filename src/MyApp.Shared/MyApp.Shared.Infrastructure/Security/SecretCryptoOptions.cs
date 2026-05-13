namespace MyApp.Shared.Infrastructure.Security;

public class SecretCryptoOptions
{
    public const string SectionName = "SecretCrypto";

    public string MasterKey { get; set; } = string.Empty;
}
