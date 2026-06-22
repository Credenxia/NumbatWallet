namespace NumbatWallet.Application.Common;

/// <summary>
/// W3C Verifiable Credentials Data Model v1.1 constants shared by the presentation token
/// issuer (Infrastructure: JwtPresentationTokenService) and the verifier
/// (Application: VerifyPresentationCommandHandler) so the two can never drift apart.
/// </summary>
public static class W3cVcConstants
{
    /// <summary>Base JSON-LD context required as the FIRST entry of every VC/VP @context.</summary>
    public const string CredentialsContextV1 = "https://www.w3.org/2018/credentials/v1";

    /// <summary>Required entry in a credential's <c>vc.type</c> array.</summary>
    public const string VerifiableCredentialType = "VerifiableCredential";

    /// <summary>Required entry in a presentation's <c>vp.type</c> array.</summary>
    public const string VerifiablePresentationType = "VerifiablePresentation";

    /// <summary>JWT claim carrying the VerifiableCredential object (VC-JWT mapping).</summary>
    public const string VcClaim = "vc";

    /// <summary>JWT claim carrying the VerifiablePresentation object (VP-JWT mapping).</summary>
    public const string VpClaim = "vp";

    /// <summary>Replay-protection nonce claim on the VP-JWT.</summary>
    public const string NonceClaim = "nonce";

    /// <summary>Private claim carrying the presentation purpose (platform extension).</summary>
    public const string PurposeClaim = "purpose";

    /// <summary>Prefix for expressing entity Guids as URIs in iss/sub/jti/credentialSubject.id.</summary>
    public const string UrnUuidPrefix = "urn:uuid:";

    /// <summary>Formats a Guid as a urn:uuid URI per RFC 9562.</summary>
    public static string ToUrnUuid(Guid id) => $"{UrnUuidPrefix}{id:D}";

    /// <summary>Parses a urn:uuid URI (or a bare Guid string) back to a Guid.</summary>
    public static bool TryParseUrnUuid(string? value, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.StartsWith(UrnUuidPrefix, StringComparison.OrdinalIgnoreCase)
            ? value[UrnUuidPrefix.Length..]
            : value;
        return Guid.TryParse(candidate, out id);
    }
}
