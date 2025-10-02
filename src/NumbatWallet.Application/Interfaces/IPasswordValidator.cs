namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Password validation interface for authentication
/// Implementations:
/// - AzureAdPasswordValidator: For government officers via Azure Entra ID
/// - ServiceWaPasswordValidator: For citizens via ServiceWA
/// - TestPasswordValidator: For integration testing only
/// </summary>
public interface IPasswordValidator
{
    /// <summary>
    /// Validates user credentials and returns roles if successful
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">Password to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Roles if authenticated, empty array if failed</returns>
    Task<string[]> ValidateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates if this validator should be used for the given email
    /// </summary>
    bool SupportsEmail(string email);
}
