using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Results;

namespace NumbatWallet.Domain.Services;

public interface IWalletService
{
    Task<Result<Wallet>> CreateWalletForPerson(Person person, Guid tenantId);
    Task<Result> IssueCredentialToWallet(Wallet wallet, Credential credential, Issuer issuer);
    Task<Result> TransferCredential(Credential credential, Wallet sourceWallet, Wallet targetWallet);
    Task<Result<string>> GenerateWalletBackupKey(Wallet wallet);
    Task<Result> RestoreWallet(string backupKey, Person person);
}