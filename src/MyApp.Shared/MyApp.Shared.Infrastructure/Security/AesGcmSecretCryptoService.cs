using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MyApp.Shared.Domain.Security;

namespace MyApp.Shared.Infrastructure.Security;

public class AesGcmSecretCryptoService : ISecretCryptoService
{
    private const byte Version = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesGcmSecretCryptoService(IOptions<SecretCryptoOptions> options)
    {
        var configured = options.Value.MasterKey?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Secret crypto master key is not configured.");

        try
        {
            _key = Convert.FromBase64String(configured);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Secret crypto master key must be a valid Base64 string.", ex);
        }

        if (_key.Length != 32)
            throw new InvalidOperationException("Secret crypto master key must be 32 bytes (Base64-encoded).");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("Value is required.", nameof(plainText));

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plainText.Trim());
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        var payload = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        payload[0] = Version;
        Buffer.BlockCopy(nonce, 0, payload, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, 1 + NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Value is required.", nameof(cipherText));

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(cipherText.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Encrypted secret payload is not valid Base64.", ex);
        }

        if (payload.Length <= 1 + NonceSize + TagSize)
            throw new InvalidOperationException("Encrypted secret payload is invalid.");

        if (payload[0] != Version)
            throw new InvalidOperationException("Encrypted secret version is not supported.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[payload.Length - 1 - NonceSize - TagSize];

        Buffer.BlockCopy(payload, 1, nonce, 0, NonceSize);
        Buffer.BlockCopy(payload, 1 + NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(payload, 1 + NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Encrypted secret payload cannot be decrypted.", ex);
        }

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }
}
