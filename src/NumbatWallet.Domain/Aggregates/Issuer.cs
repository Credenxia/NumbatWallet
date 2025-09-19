using System.Text.RegularExpressions;
using NumbatWallet.Domain.Entities;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Attributes;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Aggregates;

public sealed partial class Issuer : AuditableEntity<Guid>, ITenantAware
{
    private readonly List<string> _trustedDomains = new();
    private readonly Dictionary<string, string> _supportedCredentialTypes = new();

    public string TenantId { get; private set; } = string.Empty;
    public string? ExternalId { get; private set; }

    [DataClassification(DataClassification.Official, "Organization")]
    public string Name { get; private set; }

    [DataClassification(DataClassification.Official, "Organization")]
    public string Code { get; private set; }

    [DataClassification(DataClassification.Official, "Organization")]
    public string Description { get; private set; }

    [DataClassification(DataClassification.Official, "Organization")]
    public string IssuerDid { get; private set; }

    [DataClassification(DataClassification.Official, "Security")]
    public string PublicKey { get; private set; }

    [DataClassification(DataClassification.Official, "Security")]
    public string Endpoint { get; private set; }

    [DataClassification(DataClassification.Official, "Security")]
    public string TrustedDomain { get; private set; }
    public bool IsActive { get; private set; }
    public string? DeactivationReason { get; private set; }
    public IssuerStatus Status { get; private set; }
    public bool IsTrusted { get; private set; }
    public int TrustLevel { get; private set; }
    public string? Jurisdiction { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public DateTimeOffset? CertificateExpiresAt { get; private set; }
    public IReadOnlyCollection<RevocationRegistry> RevocationRegistries { get; } = new List<RevocationRegistry>();
    public IReadOnlyCollection<SupportedCredentialType> SupportedCredentialTypes { get; } = new List<SupportedCredentialType>();

    public IReadOnlyCollection<string> GetTrustedDomains() => _trustedDomains.AsReadOnly();
    public IReadOnlyCollection<string> GetSupportedCredentialTypes() => _supportedCredentialTypes.Keys.ToList().AsReadOnly();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Issuer() : base(Guid.Empty)
    {
        // Required for EF Core
    }
#pragma warning restore CS8618

    // Public constructor for string tenantId
    public Issuer(
        string name,
        string code,
        string issuerDid,
        string publicKey,
        string endpoint,
        string tenantId)
        : base(Guid.NewGuid())
    {
        Name = name;
        Code = code;
        IssuerDid = issuerDid;
        PublicKey = publicKey;
        Endpoint = endpoint;
        TenantId = tenantId;
        Status = IssuerStatus.Pending;
        IsTrusted = false;
        TrustLevel = 0;
        IsActive = true;
        TrustedDomain = string.Empty;
        Description = string.Empty;
    }

    private Issuer(
        Guid tenantId,
        string name,
        string code,
        string description,
        string issuerDid,
        string publicKey,
        string trustedDomain,
        IEnumerable<string> trustedDomains)
        : base(Guid.NewGuid())
    {
        TenantId = tenantId == Guid.Empty ? string.Empty : tenantId.ToString();
        Name = name;
        Code = code;
        Description = description;
        IssuerDid = issuerDid;
        PublicKey = publicKey;
        TrustedDomain = trustedDomain;
        IsActive = true;
        _trustedDomains.AddRange(trustedDomains);
        Endpoint = string.Empty; // Initialize Endpoint
        Status = IssuerStatus.Pending;
        IsTrusted = false;
        TrustLevel = 0;
    }

    public static Result<Issuer> Create(
        string name,
        string code,
        string trustedDomain)
    {
        try
        {
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));
            Guard.AgainstNullOrWhiteSpace(code, nameof(code));
            Guard.AgainstNullOrWhiteSpace(trustedDomain, nameof(trustedDomain));

            var issuer = new Issuer(
                Guid.Empty, // Will be set by DbContext
                name,
                code,
                string.Empty, // Description
                $"did:web:{trustedDomain}", // IssuerDid
                string.Empty, // PublicKey - to be set later
                trustedDomain,
                new[] { trustedDomain });

            return Result.Success(issuer);
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Issuer.Invalid", ex.Message);
        }
    }

    public void SetTenantId(string tenantId)
    {
        Guard.AgainstNullOrWhiteSpace(tenantId, nameof(tenantId));
        TenantId = tenantId;
    }

    public Result AddSupportedCredentialType(string credentialType, string schemaUrl)
    {
        Guard.AgainstNullOrWhiteSpace(credentialType, nameof(credentialType));
        Guard.AgainstNullOrWhiteSpace(schemaUrl, nameof(schemaUrl));

        if (_supportedCredentialTypes.ContainsKey(credentialType))
        {
            return DomainError.BusinessRule("Issuer.CredentialTypeExists", "Credential type is already supported.");
        }

        if (!IsActive)
        {
            return DomainError.BusinessRule("Issuer.NotActive", "Cannot add credential types to inactive issuer.");
        }

        _supportedCredentialTypes.Add(credentialType, schemaUrl);
        return Result.Success();
    }

    public Result RemoveSupportedCredentialType(string credentialType)
    {
        Guard.AgainstNullOrWhiteSpace(credentialType, nameof(credentialType));

        if (!_supportedCredentialTypes.ContainsKey(credentialType))
        {
            return DomainError.BusinessRule("Issuer.CredentialTypeNotFound", "Credential type is not supported.");
        }

        _supportedCredentialTypes.Remove(credentialType);
        return Result.Success();
    }

    public bool SupportsCredentialType(string credentialType)
    {
        return _supportedCredentialTypes.ContainsKey(credentialType);
    }

    public string? GetSchemaForCredentialType(string credentialType)
    {
        return _supportedCredentialTypes.TryGetValue(credentialType, out var schema) ? schema : null;
    }

    public Result AddTrustedDomain(string domain)
    {
        Guard.AgainstNullOrWhiteSpace(domain, nameof(domain));

        if (_trustedDomains.Contains(domain))
        {
            return DomainError.BusinessRule("Issuer.DomainExists", "Domain is already trusted.");
        }

        _trustedDomains.Add(domain);
        return Result.Success();
    }

    public Result RemoveTrustedDomain(string domain)
    {
        Guard.AgainstNullOrWhiteSpace(domain, nameof(domain));

        if (!_trustedDomains.Contains(domain))
        {
            return DomainError.BusinessRule("Issuer.DomainNotFound", "Domain is not in trusted list.");
        }

        _trustedDomains.Remove(domain);
        return Result.Success();
    }

    public bool IsDomainTrusted(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        // Check exact match
        if (_trustedDomains.Contains(domain))
        {
            return true;
        }

        // Check wildcard patterns
        foreach (var trustedDomain in _trustedDomains)
        {
            if (trustedDomain.StartsWith("*.", StringComparison.Ordinal))
            {
                var pattern = WildcardToRegex(trustedDomain);
                if (Regex.IsMatch(domain, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public Result UpdatePublicKey(string newPublicKey)
    {
        Guard.AgainstNullOrWhiteSpace(newPublicKey, nameof(newPublicKey));

        if (PublicKey == newPublicKey)
        {
            return DomainError.BusinessRule("Issuer.SamePublicKey", "New public key is the same as current key.");
        }

        PublicKey = newPublicKey;
        return Result.Success();
    }

    public Result UpdateDetails(string name, string description)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        Name = name;
        Description = description;
        return Result.Success();
    }

    public Result Deactivate(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (!IsActive)
        {
            return DomainError.BusinessRule("Issuer.AlreadyInactive", "Issuer is already inactive.");
        }

        IsActive = false;
        DeactivationReason = reason;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
        {
            return DomainError.BusinessRule("Issuer.AlreadyActive", "Issuer is already active.");
        }

        IsActive = true;
        DeactivationReason = null;
        return Result.Success();
    }

    public Result RemoveTrust()
    {
        if (!IsTrusted)
        {
            return DomainError.BusinessRule("Issuer.NotTrusted", "Issuer is not trusted.");
        }

        IsTrusted = false;
        TrustLevel = 0;
        return Result.Success();
    }

    public Result MarkAsTrusted(int trustLevel)
    {
        if (trustLevel < 0 || trustLevel > 10)
        {
            return DomainError.Validation("Issuer.InvalidTrustLevel", "Trust level must be between 0 and 10.");
        }

        if (!IsActive)
        {
            return DomainError.BusinessRule("Issuer.NotActive", "Cannot mark inactive issuer as trusted.");
        }

        IsTrusted = true;
        TrustLevel = trustLevel;
        return Result.Success();
    }

    public void SetExternalId(string? externalId)
    {
        ExternalId = externalId;
    }

    public void SetJurisdiction(string? jurisdiction)
    {
        Jurisdiction = jurisdiction;
    }

    public void SetWebsiteUrl(string? websiteUrl)
    {
        WebsiteUrl = websiteUrl;
    }

    private static string WildcardToRegex(string pattern)
    {
        var escapedPattern = Regex.Escape(pattern).Replace("\\*", ".*");
        return $"^{escapedPattern}$";
    }
}