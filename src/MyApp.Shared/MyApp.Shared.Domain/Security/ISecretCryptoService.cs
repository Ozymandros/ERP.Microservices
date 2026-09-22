namespace MyApp.Shared.Domain.Security;

/// <summary>Defines the contract for symmetric encryption and decryption of secret values.</summary>
public interface ISecretCryptoService
{
    /// <summary>Encrypts the given plain-text string and returns the result as a Base64-encoded cipher text.</summary>
    /// <param name="plainText">The plain-text value to encrypt.</param>
    /// <returns>A Base64-encoded cipher text string.</returns>
    string Encrypt(string plainText);

    /// <summary>Decrypts the given Base64-encoded cipher text and returns the original plain-text string.</summary>
    /// <param name="cipherText">The Base64-encoded cipher text to decrypt.</param>
    /// <returns>The decrypted plain-text value.</returns>
    string Decrypt(string cipherText);
}
