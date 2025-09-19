using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.ValueObjects;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Domain.Tests.Aggregates;

public class PersonTests
{
    [Fact]
    public void Person_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var email = Email.Create("john.doe@example.com");
        var phone = PhoneNumber.Create("+61412345678");
        var firstName = "John";
        var lastName = "Doe";
        var dateOfBirth = new DateOnly(1990, 1, 1);

        // Act
        var result = Person.Create(
            email,
            phone,
            firstName,
            lastName,
            dateOfBirth
        );

        // Assert
        Assert.True(result.IsSuccess);
        var person = result.Value;
        Assert.NotEqual(Guid.Empty, person.Id);
        Assert.Equal(email, person.Email);
        Assert.Equal(phone, person.PhoneNumber);
        Assert.Equal(firstName, person.FirstName);
        Assert.Equal(lastName, person.LastName);
        Assert.Equal(dateOfBirth, person.DateOfBirth);
        Assert.Equal(VerificationStatus.NotVerified, person.EmailVerificationStatus);
        Assert.Equal(VerificationStatus.NotVerified, person.PhoneVerificationStatus);
        Assert.False(person.IsVerified);
    }

    [Fact]
    public void Person_UpdateEmail_ShouldUpdateAndResetVerification()
    {
        // Arrange
        var person = CreateTestPerson();
        person.VerifyEmail("123456"); // Verify original email
        var newEmail = Email.Create("new.email@example.com");

        // Act
        var result = person.UpdateEmail(newEmail);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newEmail, person.Email);
        Assert.Equal(VerificationStatus.NotVerified, person.EmailVerificationStatus);
    }

    [Fact]
    public void Person_UpdatePhoneNumber_ShouldUpdateAndResetVerification()
    {
        // Arrange
        var person = CreateTestPerson();
        person.VerifyPhone("123456"); // Verify original phone
        var newPhone = PhoneNumber.Create("+61498765432");

        // Act
        var result = person.UpdatePhoneNumber(newPhone);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newPhone, person.PhoneNumber);
        Assert.Equal(VerificationStatus.NotVerified, person.PhoneVerificationStatus);
    }

    [Fact]
    public void Person_RequestEmailVerification_ShouldGenerateCode()
    {
        // Arrange
        var person = CreateTestPerson();

        // Act
        var code = person.RequestEmailVerification();

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.Equal(VerificationStatus.Pending, person.EmailVerificationStatus);
    }

    [Fact]
    public void Person_VerifyEmail_WithCorrectCode_ShouldVerify()
    {
        // Arrange
        var person = CreateTestPerson();
        var code = person.RequestEmailVerification();

        // Act
        var result = person.VerifyEmail(code);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationStatus.Verified, person.EmailVerificationStatus);
    }

    [Fact]
    public void Person_VerifyEmail_WithIncorrectCode_ShouldFail()
    {
        // Arrange
        var person = CreateTestPerson();
        person.RequestEmailVerification();

        // Act
        var result = person.VerifyEmail("wrong");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(VerificationStatus.Failed, person.EmailVerificationStatus);
    }

    [Fact]
    public void Person_RequestPhoneVerification_ShouldGenerateCode()
    {
        // Arrange
        var person = CreateTestPerson();

        // Act
        var code = person.RequestPhoneVerification();

        // Assert
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.Equal(VerificationStatus.Pending, person.PhoneVerificationStatus);
    }

    [Fact]
    public void Person_VerifyPhone_WithCorrectCode_ShouldVerify()
    {
        // Arrange
        var person = CreateTestPerson();
        var code = person.RequestPhoneVerification();

        // Act
        var result = person.VerifyPhone(code);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(VerificationStatus.Verified, person.PhoneVerificationStatus);
    }

    [Fact]
    public void Person_IsVerified_ShouldRequireBothEmailAndPhone()
    {
        // Arrange
        var person = CreateTestPerson();

        // Act & Assert - Not verified initially
        Assert.False(person.IsVerified);

        // Verify email only
        var emailCode = person.RequestEmailVerification();
        person.VerifyEmail(emailCode);
        Assert.False(person.IsVerified);

        // Verify phone too
        var phoneCode = person.RequestPhoneVerification();
        person.VerifyPhone(phoneCode);
        Assert.True(person.IsVerified);
    }

    [Fact]
    public void Person_UpdatePersonalDetails_ShouldUpdateNames()
    {
        // Arrange
        var person = CreateTestPerson();
        var newFirstName = "Jane";
        var newLastName = "Smith";

        // Act
        var result = person.UpdatePersonalDetails(newFirstName, newLastName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newFirstName, person.FirstName);
        Assert.Equal(newLastName, person.LastName);
    }

    [Fact]
    public void Person_GetFullName_ShouldCombineNames()
    {
        // Arrange
        var person = CreateTestPerson();

        // Act
        var fullName = person.GetFullName();

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void Person_GetAge_ShouldCalculateCorrectly()
    {
        // Arrange
        var dateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25));
        var person = Person.Create(
            Email.Create("test@example.com"),
            PhoneNumber.Create("+61412345678"),
            "Test",
            "User",
            dateOfBirth
        ).Value;

        // Act
        var age = person.GetAge();

        // Assert
        Assert.Equal(25, age);
    }

    private static Person CreateTestPerson()
    {
        return Person.Create(
            Email.Create("test@example.com"),
            PhoneNumber.Create("+61412345678"),
            "John",
            "Doe",
            new DateOnly(1990, 1, 1)
        ).Value;
    }
}