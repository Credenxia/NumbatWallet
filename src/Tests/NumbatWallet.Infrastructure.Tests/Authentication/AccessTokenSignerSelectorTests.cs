using Microsoft.Extensions.Configuration;
using NumbatWallet.Infrastructure.Authentication;

namespace NumbatWallet.Infrastructure.Tests.Authentication;

public class AccessTokenSignerSelectorTests
{
    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("Testing")]
    public void Select_DevOrTestEnvironment_NoSignerConfigured_ReturnsHmac(string environment)
    {
        var configuration = BuildConfiguration();

        var result = AccessTokenSignerSelector.Select(environment, configuration);

        result.Should().Be(AccessTokenSignerKind.Hmac);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Select_DevOrTestEnvironment_ExplicitHmac_ReturnsHmac(string environment)
    {
        var configuration = BuildConfiguration(("Jwt:Signer", "Hmac"));

        var result = AccessTokenSignerSelector.Select(environment, configuration);

        result.Should().Be(AccessTokenSignerKind.Hmac);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Select_KeyVaultRsaConfiguredWithVaultUri_ReturnsKeyVaultRsa(string environment)
    {
        var configuration = BuildConfiguration(
            ("Jwt:Signer", "KeyVaultRsa"),
            ("Jwt:SigningKeyVaultUri", "https://nw-kv.vault.azure.net/"));

        var result = AccessTokenSignerSelector.Select(environment, configuration);

        result.Should().Be(AccessTokenSignerKind.KeyVaultRsa);
    }

    [Fact]
    public void Select_KeyVaultRsaConfiguredWithFallbackKeyVaultUri_ReturnsKeyVaultRsa()
    {
        var configuration = BuildConfiguration(
            ("Jwt:Signer", "KeyVaultRsa"),
            ("KeyVault:Uri", "https://nw-kv.vault.azure.net/"));

        var result = AccessTokenSignerSelector.Select("Production", configuration);

        result.Should().Be(AccessTokenSignerKind.KeyVaultRsa);
    }

    [Fact]
    public void Select_KeyVaultRsaConfiguredWithoutVaultUri_ThrowsAtStartup()
    {
        var configuration = BuildConfiguration(("Jwt:Signer", "KeyVaultRsa"));

        var act = () => AccessTokenSignerSelector.Select("Production", configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:SigningKeyVaultUri*");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("SomeCustomEnv")]
    public void Select_NonDevEnvironment_NoSignerConfigured_FailsClosed(string environment)
    {
        var configuration = BuildConfiguration();

        var act = () => AccessTokenSignerSelector.Select(environment, configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*KeyVaultRsa*");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Select_NonDevEnvironment_ExplicitHmac_FailsClosed(string environment)
    {
        // Symmetric HS256 must never be selectable outside Development/Testing,
        // even when explicitly requested.
        var configuration = BuildConfiguration(("Jwt:Signer", "Hmac"));

        var act = () => AccessTokenSignerSelector.Select(environment, configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Select_UnknownSignerValue_Throws(string environment)
    {
        var configuration = BuildConfiguration(("Jwt:Signer", "SomethingElse"));

        var act = () => AccessTokenSignerSelector.Select(environment, configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Signer*");
    }

    [Fact]
    public void Select_NullEnvironment_FailsClosed()
    {
        var configuration = BuildConfiguration();

        var act = () => AccessTokenSignerSelector.Select(null, configuration);

        act.Should().Throw<InvalidOperationException>();
    }
}
