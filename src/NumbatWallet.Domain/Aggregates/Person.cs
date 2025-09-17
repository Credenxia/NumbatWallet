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

    public VerificationStatus EmailVerificationStatus { get; private set; }
    public VerificationStatus PhoneVerificationStatus { get; private set; }
    public bool IsVerified => EmailVerificationStatus == VerificationStatus.Verified
                           && PhoneVerificationStatus == VerificationStatus.Verified;
    public DateTimeOffset? VerifiedAt { get; private set; }
    public VerificationLevel? VerificationLevel { get; private set; }
    public Guid TenantId { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Person() : base(Guid.Empty)
    {
        // Required for EF Core
    }
#pragma warning restore CS8618

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
        EmailVerificationStatus = VerificationStatus.NotVerified;
        PhoneVerificationStatus = VerificationStatus.NotVerified;
    }

    public static Result<Person> Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
            return emailResult.Error;

        var phoneResult = PhoneNumber.Create(phoneNumber);
        if (phoneResult.IsFailure)
            return phoneResult.Error;

        return Create(emailResult.Value, phoneResult.Value, firstName, lastName, DateOnly.FromDateTime(DateTime.Now.AddYears(-25)));
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
                return Error.Validation("Person.InvalidDateOfBirth", "Date of birth cannot be in the future.");
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
            return Error.Validation("Person.Invalid", ex.Message);
        }
    }

    public Result UpdateEmail(Email newEmail)
    {
        Guard.AgainstNull(newEmail, nameof(newEmail));

        if (Email.Equals(newEmail))
        {
            return Error.BusinessRule("Person.SameEmail", "New email is the same as current email.");
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
            return Error.BusinessRule("Person.SamePhoneNumber", "New phone number is the same as current phone number.");
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
            return Error.BusinessRule("Person.NoEmailVerification", "No email verification requested.");
        }

        if (_emailCodeExpiry < DateTimeOffset.UtcNow)
        {
            EmailVerificationStatus = VerificationStatus.Failed;
            return Error.BusinessRule("Person.EmailCodeExpired", "Email verification code has expired.");
        }

        if (_emailVerificationCode != code)
        {
            EmailVerificationStatus = VerificationStatus.Failed;
            return Error.BusinessRule("Person.InvalidEmailCode", "Invalid email verification code.");
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
            return Error.BusinessRule("Person.NoPhoneVerification", "No phone verification requested.");
        }

        if (_phoneCodeExpiry < DateTimeOffset.UtcNow)
        {
            PhoneVerificationStatus = VerificationStatus.Failed;
            return Error.BusinessRule("Person.PhoneCodeExpired", "Phone verification code has expired.");
        }

        if (_phoneVerificationCode != code)
        {
            PhoneVerificationStatus = VerificationStatus.Failed;
            return Error.BusinessRule("Person.InvalidPhoneCode", "Invalid phone verification code.");
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
            return Error.Validation("Person.InvalidDetails", ex.Message);
        }
    }

    public string GetFullName() => $"{FirstName} {LastName}";

    public int GetAge()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth > today.AddYears(-age)) age--;
        return age;
    }

    private static string GenerateVerificationCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}