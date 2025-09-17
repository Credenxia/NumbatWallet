using System.Text.RegularExpressions;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Aggregates;

public sealed partial class Issuer : AuditableEntity<Guid>, ITenantAware
{
    private readonly List<string> _trustedDomains = new();
    private readonly Dictionary<string, string> _supportedCredentialTypes = new();

    public Guid TenantId { get; set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string IssuerDid { get; private set; }
    public string PublicKey { get; private set; }
    public bool IsActive { get; private set; }
    public string? DeactivationReason { get; private set; }

    public IReadOnlyCollection<string> GetTrustedDomains() => _trustedDomains.AsReadOnly();
    public IReadOnlyCollection<string> GetSupportedCredentialTypes() => _supportedCredentialTypes.Keys.ToList().AsReadOnly();

    private Issuer(
        Guid tenantId,
        string name,
        string description,
        string issuerDid,
        string publicKey,
        IEnumerable<string> trustedDomains)
        : base(Guid.NewGuid())
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IssuerDid = issuerDid;
        PublicKey = publicKey;
        IsActive = true;
        _trustedDomains.AddRange(trustedDomains);
    }

    public static Result<Issuer> Create(
        Guid tenantId,
        string name,
        string description,
        string issuerDid,
        string publicKey,
        IEnumerable<string> trustedDomains)
    {
        try
        {
            Guard.AgainstEmptyGuid(tenantId, nameof(tenantId));
            Guard.AgainstNullOrWhiteSpace(name, nameof(name));
            Guard.AgainstNullOrWhiteSpace(description, nameof(description));
            Guard.AgainstNullOrWhiteSpace(issuerDid, nameof(issuerDid));
            Guard.AgainstNullOrWhiteSpace(publicKey, nameof(publicKey));
            Guard.AgainstNull(trustedDomains, nameof(trustedDomains));

            var issuer = new Issuer(
                tenantId,
                name,
                description,
                issuerDid,
                publicKey,
                trustedDomains);

            return Result.Success(issuer);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation("Issuer.Invalid", ex.Message);
        }
    }

    public Result AddSupportedCredentialType(string credentialType, string schemaUrl)
    {
        Guard.AgainstNullOrWhiteSpace(credentialType, nameof(credentialType));
        Guard.AgainstNullOrWhiteSpace(schemaUrl, nameof(schemaUrl));

        if (_supportedCredentialTypes.ContainsKey(credentialType))
        {
            return Error.BusinessRule("Issuer.CredentialTypeExists", "Credential type is already supported.");
        }

        if (!IsActive)
        {
            return Error.BusinessRule("Issuer.NotActive", "Cannot add credential types to inactive issuer.");
        }

        _supportedCredentialTypes.Add(credentialType, schemaUrl);
        return Result.Success();
    }

    public Result RemoveSupportedCredentialType(string credentialType)
    {
        Guard.AgainstNullOrWhiteSpace(credentialType, nameof(credentialType));

        if (!_supportedCredentialTypes.ContainsKey(credentialType))
        {
            return Error.BusinessRule("Issuer.CredentialTypeNotFound", "Credential type is not supported.");
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
            return Error.BusinessRule("Issuer.DomainExists", "Domain is already trusted.");
        }

        _trustedDomains.Add(domain);
        return Result.Success();
    }

    public Result RemoveTrustedDomain(string domain)
    {
        Guard.AgainstNullOrWhiteSpace(domain, nameof(domain));

        if (!_trustedDomains.Contains(domain))
        {
            return Error.BusinessRule("Issuer.DomainNotFound", "Domain is not in trusted list.");
        }

        _trustedDomains.Remove(domain);
        return Result.Success();
    }

    public bool IsDomainTrusted(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Check exact match
        if (_trustedDomains.Contains(domain))
            return true;

        // Check wildcard patterns
        foreach (var trustedDomain in _trustedDomains)
        {
            if (trustedDomain.StartsWith("*."))
            {
                var pattern = WildcardToRegex(trustedDomain);
                if (Regex.IsMatch(domain, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public Result UpdatePublicKey(string newPublicKey)
    {
        Guard.AgainstNullOrWhiteSpace(newPublicKey, nameof(newPublicKey));

        if (PublicKey == newPublicKey)
        {
            return Error.BusinessRule("Issuer.SamePublicKey", "New public key is the same as current key.");
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
            return Error.BusinessRule("Issuer.AlreadyInactive", "Issuer is already inactive.");
        }

        IsActive = false;
        DeactivationReason = reason;
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (IsActive)
        {
            return Error.BusinessRule("Issuer.AlreadyActive", "Issuer is already active.");
        }

        IsActive = true;
        DeactivationReason = null;
        return Result.Success();
    }

    private static string WildcardToRegex(string pattern)
    {
        var escapedPattern = Regex.Escape(pattern).Replace("\\*", ".*");
        return $"^{escapedPattern}$";
    }
}