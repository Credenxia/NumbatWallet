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
    /// Validates user credentials against the identity provider
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">Password to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The authenticated user's roles when validation SUCCEEDS (a non-empty array signals success).
    /// An EMPTY array when validation fails. Implementations must never return a non-empty array on failure,
    /// because <c>LoginCommandHandler</c> treats any non-empty result as a successful authentication.
    /// </returns>
    Task<string[]> ValidateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates if this validator should be used for the given email
    /// </summary>
    bool SupportsEmail(string email);
}
