using Microsoft.Extensions.Configuration;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// The access-token signing implementation selected for the current host.
/// </summary>
public enum AccessTokenSignerKind
{
    /// <summary>
    /// Symmetric HS256 (<c>HmacAccessTokenSigner</c>). Development/Testing ONLY — anyone who can
    /// validate a token can also mint one, so it must never run outside local environments.
    /// </summary>
    Hmac,

    /// <summary>
    /// Asymmetric RS256 with the private key sourced from Azure Key Vault
    /// (<see cref="KeyVaultRsaAccessTokenSigner"/>). Required outside Development/Testing.
    /// </summary>
    KeyVaultRsa,
}

/// <summary>
/// Explicit, fail-closed selection of the access-token signer:
/// <list type="bullet">
///   <item>Development/Testing default to HS256 (HMAC) unless <c>Jwt:Signer=KeyVaultRsa</c> opts in to RS256.</item>
///   <item>Any other environment REQUIRES <c>Jwt:Signer=KeyVaultRsa</c> plus a Key Vault URI
///   (<c>Jwt:SigningKeyVaultUri</c> or <c>KeyVault:Uri</c>); otherwise startup fails fast.</item>
/// </list>
/// KEY ROTATION: the JwtBearer validator is configured from
/// <c>IAccessTokenSigner.GetValidationKeys()</c> at startup. Rotating the Key Vault signing key
/// therefore requires a rolling restart, and tokens issued under the OLD key become invalid the
/// moment an instance only trusts the new key. To rotate without a hard logout, publish the old
/// key alongside the new one (GetValidationKeys returning both, distinguished by <c>kid</c>) for
/// at least one access-token lifetime before retiring it.
/// </summary>
public static class AccessTokenSignerSelector
{
    /// <summary>
    /// Resolves which signer the host must use, throwing <see cref="InvalidOperationException"/>
    /// at startup (fail fast, fail closed) when a non-local environment is missing the required
    /// asymmetric signing configuration.
    /// </summary>
    public static AccessTokenSignerKind Select(string? environmentName, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration["Jwt:Signer"];
        var isDevOrTest =
            string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(configured, "KeyVaultRsa", StringComparison.OrdinalIgnoreCase))
        {
            // Fail fast: verify the vault URI now rather than on the first token issuance.
            var vaultUri = configuration["Jwt:SigningKeyVaultUri"] ?? configuration["KeyVault:Uri"];
            if (string.IsNullOrWhiteSpace(vaultUri))
            {
                throw new InvalidOperationException(
                    "'Jwt:Signer' is 'KeyVaultRsa' but neither 'Jwt:SigningKeyVaultUri' nor " +
                    "'KeyVault:Uri' is configured. The RS256 signing key cannot be loaded.");
            }

            return AccessTokenSignerKind.KeyVaultRsa;
        }

        if (!string.IsNullOrWhiteSpace(configured)
            && !string.Equals(configured, "Hmac", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unrecognized 'Jwt:Signer' value '{configured}'. " +
                "Valid values: 'KeyVaultRsa' (required outside Development/Testing) or 'Hmac' (Development/Testing only).");
        }

        if (isDevOrTest)
        {
            return AccessTokenSignerKind.Hmac;
        }

        // Fail closed: symmetric HS256 is never acceptable outside local environments,
        // whether by omission or by explicit 'Jwt:Signer=Hmac'.
        throw new InvalidOperationException(
            $"Access-token signing is not configured for environment '{environmentName}'. " +
            "Outside Development/Testing the symmetric HS256 dev signer is not allowed: set " +
            "'Jwt:Signer' to 'KeyVaultRsa' and configure 'Jwt:SigningKeyVaultUri' (or 'KeyVault:Uri') " +
            "and 'Jwt:SigningKeySecretName' so RS256 tokens are signed via Azure Key Vault.");
    }
}
