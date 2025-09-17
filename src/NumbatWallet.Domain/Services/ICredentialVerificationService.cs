using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Domain.Services;

public interface ICredentialVerificationService
{
    Task<Result<bool>> VerifyCredentialSignature(Credential credential, Issuer issuer);
    Task<Result<bool>> ValidateCredentialSchema(Credential credential, string schemaUrl);
    Task<Result> CheckCredentialRevocationStatus(Credential credential, Issuer issuer);
}