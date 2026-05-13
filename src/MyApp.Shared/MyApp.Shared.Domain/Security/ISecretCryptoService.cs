namespace MyApp.Shared.Domain.Security;

public interface ISecretCryptoService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
