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
        Assert.NotNull(phone);
        Assert.Equal(validPhone, phone.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PhoneNumber_WithNullOrWhitespace_ShouldThrowArgumentException(string invalidPhone)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(invalidPhone));
    }

    [Theory]
    [InlineData("123456")] // Too short
    [InlineData("0412345678")] // Missing country code
    [InlineData("61412345678")] // Missing +
    [InlineData("+123")] // Too short
    [InlineData("+abc123")] // Contains letters
    public void PhoneNumber_WithInvalidFormat_ShouldThrowArgumentException(string invalidPhone)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => PhoneNumber.Create(invalidPhone));
        Assert.Contains("Invalid phone number format", ex.Message);
    }

    [Fact]
    public void PhoneNumber_Equality_ShouldWork()
    {
        // Arrange
        var phone1 = PhoneNumber.Create("+61412345678");
        var phone2 = PhoneNumber.Create("+61412345678");
        var phone3 = PhoneNumber.Create("+61498765432");

        // Act & Assert
        Assert.Equal(phone1, phone2);
        Assert.NotEqual(phone1, phone3);
    }

    [Fact]
    public void PhoneNumber_WithCountryCode_ShouldFormatCorrectly()
    {
        // Arrange & Act
        var phone = PhoneNumber.Create("412345678", "61");

        // Assert
        Assert.Equal("+61412345678", phone.Value);
    }

    [Fact]
    public void PhoneNumber_WithSpacesAndDashes_ShouldClean()
    {
        // Arrange & Act
        var phone = PhoneNumber.Create("+61 4-123-456-78");

        // Assert
        Assert.Equal("+61412345678", phone.Value);
    }

    [Fact]
    public void PhoneNumber_ToString_ShouldReturnValue()
    {
        // Arrange
        var phone = PhoneNumber.Create("+61412345678");

        // Act
        var result = phone.ToString();

        // Assert
        Assert.Equal("+61412345678", result);
    }

    [Fact]
    public void PhoneNumber_GetFormatted_WithUSNumber_ShouldFormatCorrectly()
    {
        // Arrange
        var phone = PhoneNumber.Create("+11234567890"); // US number with country code

        // Act
        var formatted = phone.GetFormatted();

        // Assert
        // For international numbers, we typically return as-is or with basic formatting
        Assert.Equal("+1 123 456 7890", formatted);
    }

    [Fact]
    public void PhoneNumber_GetFormatted_WithAustralianNumber_ShouldFormatCorrectly()
    {
        // Arrange
        var phone = PhoneNumber.Create("+61412345678");

        // Act
        var formatted = phone.GetFormatted();

        // Assert
        Assert.Equal("+61 412 345 678", formatted);
    }
}