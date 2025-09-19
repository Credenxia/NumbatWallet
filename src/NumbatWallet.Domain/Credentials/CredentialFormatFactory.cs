using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Factory for creating credential format handlers
/// </summary>
public class CredentialFormatFactory
{
    private readonly Dictionary<CredentialFormat, Func<ICredentialFormat>> _formatFactories;

    public CredentialFormatFactory()
    {
        _formatFactories = new Dictionary<CredentialFormat, Func<ICredentialFormat>>
        {
            { CredentialFormat.JwtVc, () => new JwtVcFormat() },
            { CredentialFormat.JsonLd, () => new JsonLdFormat() }
        };
    }

    public ICredentialFormat CreateFormat(CredentialFormat format)
    {
        if (!_formatFactories.ContainsKey(format))
        {
            throw new NotSupportedException($"Credential format {format} is not supported");
        }

        return _formatFactories[format]();
    }
}