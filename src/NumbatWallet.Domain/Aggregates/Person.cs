using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Primitives;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Results;
using NumbatWallet.SharedKernel.Guards;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Attributes;

namespace NumbatWallet.Domain.Aggregates;

public sealed class Person : AuditableEntity<Guid>, ITenantAware
{
    private string? _emailVerificationCode;
    private string? _phoneVerificationCode;
    private DateTimeOffset? _emailCodeExpiry;
    private DateTimeOffset? _phoneCodeExpiry;

    [DataClassification(DataClassification.OfficialSensitive, "Contact")]
    public Email Email { get; private set; }

    [DataClassification(DataClassification.OfficialSensitive, "Contact")]
    public PhoneNumber PhoneNumber { get; private set; }

    [DataClassification(DataClassification.OfficialSensitive, "Identity")]
    public string FirstName { get; private set; }

    [DataClassification(DataClassification.OfficialSensitive, "Identity")]
    public string LastName { get; private set; }

    [DataClassification(DataClassification.OfficialSensitive, "Identity")]
    public DateOnly DateOfBirth { get; private set; }

    public string ExternalId { get; private set; }
    public string? MobileNumber { get; private set; }
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public VerificationStatus EmailVerificationStatus { get; private set; }
    public VerificationStatus PhoneVerificationStatus { get; private set; }
    public bool IsVerified => EmailVerificationStatus == VerificationStatus.Verified
                           && PhoneVerificationStatus == VerificationStatus.Verified;
    public DateTimeOffset? VerifiedAt { get; private set; }
    public VerificationLevel? VerificationLevel { get; private set; }
    public PersonStatus Status { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    // Navigation properties
    private readonly List<Wallet> _wallets = new();
    public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Person() : base(Guid.Empty)
    {
        // Required for persistence
    }
#pragma warning restore CS8618

    // Public constructor for seeding
    public Person(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string email,
        string externalId,
        string tenantId)
        : base(Guid.NewGuid())
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Email = Email.Create(email);
        ExternalId = externalId;
        TenantId = tenantId;
        EmailVerificationStatus = VerificationStatus.NotVerified;
        PhoneVerificationStatus = VerificationStatus.NotVerified;
        PhoneNumber = PhoneNumber.Create("+61400000000"); // Default placeholder
        Status = PersonStatus.PendingVerification;
    }

    private Person(
        Email email,
        PhoneNumber phoneNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth)
        : base(Guid.NewGuid())
    {
        Email = email;
        PhoneNumber = phoneNumber;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        ExternalId = Guid.NewGuid().ToString();
        EmailVerificationStatus = VerificationStatus.NotVerified;
        PhoneVerificationStatus = VerificationStatus.NotVerified;
        Status = PersonStatus.PendingVerification;
    }

    public static Result<Person> Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber)
    {
        try
        {
            var emailValue = Email.Create(email);
            var phoneValue = PhoneNumber.Create(phoneNumber);

            return Create(emailValue, phoneValue, firstName, lastName, DateOnly.FromDateTime(DateTime.Now.AddYears(-25)));
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Person.InvalidInput", ex.Message);
        }
    }

    public static Result<Person> Create(
        Email email,
        PhoneNumber phoneNumber,
        string firstName,
        string lastName,
        DateOnly dateOfBirth)
    {
        try
        {
            Guard.AgainstNull(email, nameof(email));
            Guard.AgainstNull(phoneNumber, nameof(phoneNumber));
            Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
            Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));

            if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return DomainError.Validation("Person.InvalidDateOfBirth", "Date of birth cannot be in the future.");
            }

            var person = new Person(
                email,
                phoneNumber,
                firstName,
                lastName,
                dateOfBirth);

            return Result.Success(person);
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Person.Invalid", ex.Message);
        }
    }

    public Result UpdateEmail(Email newEmail)
    {
        Guard.AgainstNull(newEmail, nameof(newEmail));

        if (Email.Equals(newEmail))
        {
            return DomainError.BusinessRule("Person.SameEmail", "New email is the same as current email.");
        }

        Email = newEmail;
        EmailVerificationStatus = VerificationStatus.NotVerified;
        _emailVerificationCode = null;
        _emailCodeExpiry = null;

        return Result.Success();
    }

    public Result UpdatePhoneNumber(PhoneNumber newPhoneNumber)
    {
        Guard.AgainstNull(newPhoneNumber, nameof(newPhoneNumber));

        if (PhoneNumber.Equals(newPhoneNumber))
        {
            return DomainError.BusinessRule("Person.SamePhoneNumber", "New phone number is the same as current phone number.");
        }

        PhoneNumber = newPhoneNumber;
        PhoneVerificationStatus = VerificationStatus.NotVerified;
        _phoneVerificationCode = null;
        _phoneCodeExpiry = null;

        return Result.Success();
    }

    public string RequestEmailVerification()
    {
        _emailVerificationCode = GenerateVerificationCode();
        _emailCodeExpiry = DateTimeOffset.UtcNow.AddMinutes(15);
        EmailVerificationStatus = VerificationStatus.Pending;
        return _emailVerificationCode;
    }

    public Result VerifyEmail(string code)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));

        if (_emailVerificationCode == null || _emailCodeExpiry == null)
        {
            return DomainError.BusinessRule("Person.NoEmailVerification", "No email verification requested.");
        }

        if (_emailCodeExpiry < DateTimeOffset.UtcNow)
        {
            EmailVerificationStatus = VerificationStatus.Failed;
            return DomainError.BusinessRule("Person.EmailCodeExpired", "Email verification code has expired.");
        }

        if (_emailVerificationCode != code)
        {
            EmailVerificationStatus = VerificationStatus.Failed;
            return DomainError.BusinessRule("Person.InvalidEmailCode", "Invalid email verification code.");
        }

        EmailVerificationStatus = VerificationStatus.Verified;
        _emailVerificationCode = null;
        _emailCodeExpiry = null;
        return Result.Success();
    }

    public string RequestPhoneVerification()
    {
        _phoneVerificationCode = GenerateVerificationCode();
        _phoneCodeExpiry = DateTimeOffset.UtcNow.AddMinutes(15);
        PhoneVerificationStatus = VerificationStatus.Pending;
        return _phoneVerificationCode;
    }

    public Result VerifyPhone(string code)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));

        if (_phoneVerificationCode == null || _phoneCodeExpiry == null)
        {
            return DomainError.BusinessRule("Person.NoPhoneVerification", "No phone verification requested.");
        }

        if (_phoneCodeExpiry < DateTimeOffset.UtcNow)
        {
            PhoneVerificationStatus = VerificationStatus.Failed;
            return DomainError.BusinessRule("Person.PhoneCodeExpired", "Phone verification code has expired.");
        }

        if (_phoneVerificationCode != code)
        {
            PhoneVerificationStatus = VerificationStatus.Failed;
            return DomainError.BusinessRule("Person.InvalidPhoneCode", "Invalid phone verification code.");
        }

        PhoneVerificationStatus = VerificationStatus.Verified;
        _phoneVerificationCode = null;
        _phoneCodeExpiry = null;
        return Result.Success();
    }

    public Result UpdatePersonalDetails(string firstName, string lastName)
    {
        try
        {
            Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
            Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));

            FirstName = firstName;
            LastName = lastName;

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return DomainError.Validation("Person.InvalidDetails", ex.Message);
        }
    }

    public string GetFullName() => $"{FirstName} {LastName}";

    public int GetAge()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    public void MarkAsVerified()
    {
        Status = PersonStatus.Verified;
        VerifiedAt = DateTimeOffset.UtcNow;
        EmailVerificationStatus = VerificationStatus.Verified;
        PhoneVerificationStatus = VerificationStatus.Verified;
    }

    public void SetTenantId(string tenantId)
    {
        Guard.AgainstNullOrWhiteSpace(tenantId, nameof(tenantId));
        TenantId = tenantId;
    }

    private static string GenerateVerificationCode()
    {
        // Use cryptographically secure random number generator
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);

        // Convert to a 6-digit number (100000-999999)
        var value = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000;
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}