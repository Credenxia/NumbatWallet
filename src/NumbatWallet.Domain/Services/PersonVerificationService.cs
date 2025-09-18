using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Events;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Services;

public interface IPersonVerificationService
{
    Task<PersonVerificationResult> VerifyPersonIdentityAsync(
        Person person,
        VerificationMethod method,
        Dictionary<string, object> verificationData,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateEmailAsync(
        Email email,
        CancellationToken cancellationToken = default);

    Task<bool> ValidatePhoneNumberAsync(
        PhoneNumber phoneNumber,
        CancellationToken cancellationToken = default);

    Task<bool> IsPersonEligibleForWalletAsync(
        Person person,
        CancellationToken cancellationToken = default);
}

public class PersonVerificationService : IPersonVerificationService
{
    private readonly IPersonRepository _personRepository;
    private readonly IWalletRepository _walletRepository;

    public PersonVerificationService(
        IPersonRepository personRepository,
        IWalletRepository walletRepository)
    {
        _personRepository = personRepository;
        _walletRepository = walletRepository;
    }

    public async Task<PersonVerificationResult> VerifyPersonIdentityAsync(
        Person person,
        VerificationMethod method,
        Dictionary<string, object> verificationData,
        CancellationToken cancellationToken = default)
    {
        var result = new PersonVerificationResult
        {
            PersonId = person.Id,
            Method = method,
            VerifiedAt = DateTimeOffset.UtcNow
        };

        // Check person status
        if (person.Status != PersonStatus.Active && person.Status != PersonStatus.PendingVerification)
        {
            result.IsVerified = false;
            result.FailureReason = $"Person status is {person.Status}";
            return result;
        }

        // Perform verification based on method
        switch (method)
        {
            case VerificationMethod.Email:
                result.IsVerified = await VerifyEmailMethod(person, verificationData, cancellationToken);
                break;

            case VerificationMethod.Phone:
                result.IsVerified = await VerifyPhoneMethod(person, verificationData, cancellationToken);
                break;

            case VerificationMethod.Document:
                result.IsVerified = await VerifyDocumentMethod(person, verificationData, cancellationToken);
                break;

            case VerificationMethod.Biometric:
                result.IsVerified = await VerifyBiometricMethod(person, verificationData, cancellationToken);
                break;

            default:
                result.IsVerified = false;
                result.FailureReason = "Unsupported verification method";
                break;
        }

        if (result.IsVerified)
        {
            // Update person verification status
            person.MarkAsVerified(method.ToString(), Guid.NewGuid().ToString());

            // Add verification event
            person.AddDomainEvent(new PersonVerifiedEvent(
                person.Id,
                method.ToString(),
                result.VerificationId,
                DateTimeOffset.UtcNow));
        }

        return result;
    }

    public async Task<bool> ValidateEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        // Check if email is already in use
        var existingPerson = await _personRepository.GetByEmailAsync(email.Value, cancellationToken);
        return existingPerson == null;
    }

    public async Task<bool> ValidatePhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
    {
        // Check if phone number is already in use
        var existingPerson = await _personRepository.GetByMobileNumberAsync(phoneNumber.Value, cancellationToken);
        return existingPerson == null;
    }

    public async Task<bool> IsPersonEligibleForWalletAsync(Person person, CancellationToken cancellationToken = default)
    {
        // Check person is verified
        if (!person.IsVerified)
            return false;

        // Check person is active
        if (person.Status != PersonStatus.Active && person.Status != PersonStatus.Verified)
            return false;

        // Check person doesn't already have an active wallet
        var wallets = await _walletRepository.GetByPersonIdAsync(person.Id, cancellationToken);
        var hasActiveWallet = wallets.Any(w => w.Status == WalletStatus.Active);

        return !hasActiveWallet;
    }

    private async Task<bool> VerifyEmailMethod(
        Person person,
        Dictionary<string, object> verificationData,
        CancellationToken cancellationToken)
    {
        if (!verificationData.ContainsKey("verificationCode"))
            return false;

        var providedCode = verificationData["verificationCode"]?.ToString();
        var expectedCode = verificationData.ContainsKey("expectedCode")
            ? verificationData["expectedCode"]?.ToString()
            : null;

        // In real implementation, this would check against stored verification codes
        return !string.IsNullOrEmpty(providedCode) && providedCode == expectedCode;
    }

    private async Task<bool> VerifyPhoneMethod(
        Person person,
        Dictionary<string, object> verificationData,
        CancellationToken cancellationToken)
    {
        if (!verificationData.ContainsKey("smsCode"))
            return false;

        var providedCode = verificationData["smsCode"]?.ToString();
        var expectedCode = verificationData.ContainsKey("expectedCode")
            ? verificationData["expectedCode"]?.ToString()
            : null;

        // In real implementation, this would check against SMS verification service
        return !string.IsNullOrEmpty(providedCode) && providedCode == expectedCode;
    }

    private async Task<bool> VerifyDocumentMethod(
        Person person,
        Dictionary<string, object> verificationData,
        CancellationToken cancellationToken)
    {
        // In real implementation, this would integrate with document verification service
        // For now, check if required document data is present
        return verificationData.ContainsKey("documentType") &&
               verificationData.ContainsKey("documentNumber") &&
               verificationData.ContainsKey("documentImage");
    }

    private async Task<bool> VerifyBiometricMethod(
        Person person,
        Dictionary<string, object> verificationData,
        CancellationToken cancellationToken)
    {
        // In real implementation, this would integrate with biometric verification service
        // For now, check if biometric data is present
        return verificationData.ContainsKey("biometricType") &&
               verificationData.ContainsKey("biometricData");
    }
}

public class PersonVerificationResult
{
    public Guid PersonId { get; set; }
    public VerificationMethod Method { get; set; }
    public bool IsVerified { get; set; }
    public string? FailureReason { get; set; }
    public string? VerificationId { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum VerificationMethod
{
    Email = 1,
    Phone = 2,
    Document = 3,
    Biometric = 4,
    MultiFactorAuthentication = 5
}