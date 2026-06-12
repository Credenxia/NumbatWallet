using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NumbatWallet.Infrastructure.Crypto;
using NumbatWallet.Infrastructure.Data.Converters;

namespace NumbatWallet.Infrastructure.Tests.Crypto;

public class AesGcmFieldEncryptorTests
{
    private static readonly string TestKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());

    private static IConfiguration Config(string? keyBase64)
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["FieldEncryption:Source"]).Returns((string?)null);
        mock.Setup(c => c["FieldEncryption:KeyVaultUri"]).Returns((string?)null);
        mock.Setup(c => c["KeyVault:Uri"]).Returns((string?)null);
        mock.Setup(c => c["FieldEncryption:KeySecretName"]).Returns((string?)null);
        mock.Setup(c => c["FieldEncryption:Key"]).Returns(keyBase64);
        return mock.Object;
    }

    private static AesGcmFieldEncryptor Enabled() =>
        new(Config(TestKeyBase64), NullLogger<AesGcmFieldEncryptor>.Instance);

    private static AesGcmFieldEncryptor Disabled() =>
        new(Config(null), NullLogger<AesGcmFieldEncryptor>.Instance);

    [Fact]
    public void Encrypt_Decrypt_RoundTrips_AndCiphertextDiffersFromPlaintext()
    {
        var sut = Enabled();
        sut.IsEnabled.Should().BeTrue();

        const string plaintext = "Jane Citizen 1990-04-05";
        var token = sut.Encrypt(plaintext);

        token.Should().StartWith("FE1:");
        token.Should().NotContain(plaintext);
        sut.Decrypt(token).Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime_ButDecryptsToSame()
    {
        var sut = Enabled();
        var a = sut.Encrypt("same");
        var b = sut.Encrypt("same");
        a.Should().NotBe(b); // random nonce
        sut.Decrypt(a).Should().Be("same");
        sut.Decrypt(b).Should().Be("same");
    }

    [Fact]
    public void Decrypt_LegacyPlaintext_ReturnsUnchanged()
    {
        var sut = Enabled();
        sut.Decrypt("legacy-plaintext-no-prefix").Should().Be("legacy-plaintext-no-prefix");
    }

    [Fact]
    public void Disabled_PassesThrough()
    {
        var sut = Disabled();
        sut.IsEnabled.Should().BeFalse();
        sut.Encrypt("hello").Should().Be("hello");
        sut.Decrypt("hello").Should().Be("hello");
    }

    [Fact]
    public void ProtectedFieldConverter_StoresCiphertextNotPlaintext_AndRoundTrips()
    {
        var previous = ProtectedFieldConverter.FieldEncryptor;
        try
        {
            ProtectedFieldConverter.FieldEncryptor = Enabled();
            var converter = new ProtectedFieldConverter();

            const string pii = "Sensitive Name";
            var stored = converter.ConvertToProviderTyped(pii);

            // The JSONB payload must NOT contain the plaintext; it must contain a ciphertext token.
            stored.Should().NotContain(pii);
            stored.Should().Contain("FE1:");
            converter.ConvertFromProviderTyped(stored).Should().Be(pii);
        }
        finally
        {
            ProtectedFieldConverter.FieldEncryptor = previous;
        }
    }

    [Fact]
    public void ProtectedFieldConverter_WithoutEncryptor_RoundTripsPlaintext()
    {
        var previous = ProtectedFieldConverter.FieldEncryptor;
        try
        {
            ProtectedFieldConverter.FieldEncryptor = null;
            var converter = new ProtectedFieldConverter();
            var stored = converter.ConvertToProviderTyped("plain");
            converter.ConvertFromProviderTyped(stored).Should().Be("plain");
        }
        finally
        {
            ProtectedFieldConverter.FieldEncryptor = previous;
        }
    }
}
