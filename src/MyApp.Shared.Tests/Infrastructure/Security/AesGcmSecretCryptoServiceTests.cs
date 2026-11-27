using FluentAssertions;
using Microsoft.Extensions.Options;
using MyApp.Shared.Infrastructure.Security;
using Xunit;

namespace MyApp.Shared.Tests.Infrastructure.Security;

public class AesGcmSecretCryptoServiceTests
{
    private static string CreateValidBase64Key() => Convert.ToBase64String(new byte[32]);

    private static AesGcmSecretCryptoService CreateService(string? masterKey = null)
    {
        var options = Options.Create(new SecretCryptoOptions
        {
            MasterKey = masterKey ?? CreateValidBase64Key()
        });
        return new AesGcmSecretCryptoService(options);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalPlaintext()
    {
        var sut = CreateService();
        const string plaintext = "sk-live-secret-value";

        var cipher = sut.Encrypt(plaintext);
        var decrypted = sut.Decrypt(cipher);

        decrypted.Should().Be(plaintext);
        cipher.Should().NotBe(plaintext);
    }

    [Fact]
    public void Encrypt_TrimsPlaintext_BeforeEncrypting()
    {
        var sut = CreateService();

        var cipher = sut.Encrypt("  secret-value  ");
        var decrypted = sut.Decrypt(cipher);

        decrypted.Should().Be("secret-value");
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertexts_ForSamePlaintext()
    {
        var sut = CreateService();

        var first = sut.Encrypt("same-secret");
        var second = sut.Encrypt("same-secret");

        first.Should().NotBe(second);
        sut.Decrypt(first).Should().Be("same-secret");
        sut.Decrypt(second).Should().Be("same-secret");
    }

    [Fact]
    public void Encrypt_WithNullOrWhitespace_ThrowsArgumentException()
    {
        var sut = CreateService();

        Assert.Throws<ArgumentException>(() => sut.Encrypt(null!));
        Assert.Throws<ArgumentException>(() => sut.Encrypt("   "));
    }

    [Fact]
    public void Decrypt_WithNullOrWhitespace_ThrowsArgumentException()
    {
        var sut = CreateService();

        Assert.Throws<ArgumentException>(() => sut.Decrypt(null!));
        Assert.Throws<ArgumentException>(() => sut.Decrypt("   "));
    }

    [Fact]
    public void Decrypt_WithInvalidBase64_ThrowsInvalidOperationException()
    {
        var sut = CreateService();

        var act = () => sut.Decrypt("not-valid-base64!!!");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Encrypted secret payload is not valid Base64.");
    }

    [Fact]
    public void Decrypt_WithTooShortPayload_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var shortPayload = Convert.ToBase64String(new byte[] { 1, 2, 3 });

        var act = () => sut.Decrypt(shortPayload);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Encrypted secret payload is invalid.");
    }

    [Fact]
    public void Decrypt_WithUnsupportedVersion_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var payload = new byte[1 + 12 + 16 + 4];
        payload[0] = 99; // unsupported version
        var cipher = Convert.ToBase64String(payload);

        var act = () => sut.Decrypt(cipher);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Encrypted secret version is not supported.");
    }

    [Fact]
    public void Decrypt_WithTamperedPayload_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var cipher = sut.Encrypt("secret");
        var bytes = Convert.FromBase64String(cipher);
        bytes[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(bytes);

        var act = () => sut.Decrypt(tampered);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Encrypted secret payload cannot be decrypted.");
    }

    [Fact]
    public void Constructor_WithMissingMasterKey_ThrowsInvalidOperationException()
    {
        var act = () => CreateService("   ");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Secret crypto master key is not configured.");
    }

    [Fact]
    public void Constructor_WithInvalidBase64Key_ThrowsInvalidOperationException()
    {
        var act = () => CreateService("not-base64!");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Secret crypto master key must be a valid Base64 string.");
    }

    [Fact]
    public void Constructor_WithWrongKeyLength_ThrowsInvalidOperationException()
    {
        var shortKey = Convert.ToBase64String(new byte[16]);

        var act = () => CreateService(shortKey);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Secret crypto master key must be 32 bytes (Base64-encoded).");
    }
}
