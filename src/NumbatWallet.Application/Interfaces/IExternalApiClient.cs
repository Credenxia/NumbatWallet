namespace NumbatWallet.Application.Interfaces;

public interface IExternalApiClient
{
    Task<TResponse?> GetAsync<TResponse>(
        string endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}

public interface IIdentityVerificationApiClient
{
    Task<IdentityVerificationResult> VerifyIdentityAsync(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string documentNumber,
        string documentType,
        CancellationToken cancellationToken = default);

    Task<DocumentVerificationResult> VerifyDocumentAsync(
        byte[] documentImage,
        string documentType,
        CancellationToken cancellationToken = default);
}

public class IdentityVerificationResult
{
    public bool IsVerified { get; set; }
    public string? VerificationId { get; set; }
    public Dictionary<string, string>? Details { get; set; }
    public string? Error { get; set; }
}

public class DocumentVerificationResult
{
    public bool IsValid { get; set; }
    public string? DocumentId { get; set; }
    public Dictionary<string, string>? ExtractedData { get; set; }
    public string? Error { get; set; }
}