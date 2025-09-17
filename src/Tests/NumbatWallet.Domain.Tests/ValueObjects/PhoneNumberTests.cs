using Xunit;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+61412345678")] // Australian mobile
    [InlineData("+61298765432")] // Australian landline
    [InlineData("+1234567890")]  // International
    [InlineData("+44207890123")]  // UK
    public void PhoneNumber_WithValidValue_ShouldCreate(string validPhone)
    {
        // Act
        var phone = PhoneNumber.Create(validPhone);

        // Assert
        Assert.True(phone.IsSuccess);
        Assert.Equal(validPhone, phone.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("123456")] // Too short
    [InlineData("0412345678")] // Missing country code
    [InlineData("61412345678")] // Missing +
    [InlineData("+123")] // Too short
    [InlineData("+abc123")] // Contains letters
    public void PhoneNumber_WithInvalidValue_ShouldFail(string invalidPhone)
    {
        // Act
        var phone = PhoneNumber.Create(invalidPhone);

        // Assert
        Assert.True(phone.IsFailure);
    }

    [Fact]
    public void PhoneNumber_Equality_ShouldWork()
    {
        // Arrange
        var phone1 = PhoneNumber.Create("+61412345678").Value;
        var phone2 = PhoneNumber.Create("+61412345678").Value;
        var phone3 = PhoneNumber.Create("+61498765432").Value;

        // Act & Assert
        Assert.Equal(phone1, phone2);
        Assert.NotEqual(phone1, phone3);
    }

    [Fact]
    public void PhoneNumber_GetCountryCode_ShouldExtractCorrectly()
    {
        // Arrange
        var phone = PhoneNumber.Create("+61412345678").Value;

        // Act
        var countryCode = phone.GetCountryCode();

        // Assert
        Assert.Equal("61", countryCode);
    }
}