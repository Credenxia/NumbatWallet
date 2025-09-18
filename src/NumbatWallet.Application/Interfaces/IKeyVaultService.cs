namespace NumbatWallet.Application.Interfaces;

public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    Task<bool> SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default);

    Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default);

    Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default);

    void ClearCache();
}