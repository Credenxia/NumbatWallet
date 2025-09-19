namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Supported credential formats according to W3C Verifiable Credentials standards
/// </summary>
public enum CredentialFormat
{
    /// <summary>
    /// JWT-VC format (JSON Web Token for Verifiable Credentials)
    /// See: https://www.w3.org/TR/vc-data-model/#json-web-token
    /// </summary>
    JwtVc = 1,

    /// <summary>
    /// JSON-LD format (JSON for Linked Data)
    /// See: https://www.w3.org/TR/vc-data-model/#json-ld
    /// </summary>
    JsonLd = 2,

    /// <summary>
    /// SD-JWT format (Selective Disclosure JWT)
    /// See: https://www.ietf.org/archive/id/draft-ietf-oauth-selective-disclosure-jwt-04.html
    /// </summary>
    SdJwt = 3,

    /// <summary>
    /// Mobile Document format (ISO/IEC 18013-5)
    /// For mobile driving licenses
    /// </summary>
    MDoc = 4
}