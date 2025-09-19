using Xunit;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    // Australian numbers
    [InlineData("+61412345678", "+61412345678")] // Mobile
    [InlineData("+61 412 345 678", "+61412345678")] // With spaces
    [InlineData("+61 2 9876 5432", "+61298765432")] // Sydney landline
    [InlineData("0412345678", "+61412345678", "AU")] // National format with country
    [InlineData("02 9876 5432", "+61298765432", "AU")] // National landline
    // US numbers
    [InlineData("+14155552671", "+14155552671")] // San Francisco
    [InlineData("(415) 555-2671", "+14155552671", "US")] // National format
    [InlineData("415-555-2671", "+14155552671", "US")] // With dashes
    [InlineData("+1 415 555 2671", "+14155552671")] // With spaces
    // UK numbers
    [InlineData("+442071234567", "+442071234567")] // London
    [InlineData("020 7123 4567", "+442071234567", "GB")] // National format
    [InlineData("+44 20 7123 4567", "+442071234567")] // With spaces
    // Brazil numbers
    [InlineData("+5511987654321", "+5511987654321")] // São Paulo mobile
    [InlineData("11 98765-4321", "+5511987654321", "BR")] // National format
    // Germany numbers
    [InlineData("+491701234567", "+491701234567")] // Mobile
    [InlineData("0170 1234567", "+491701234567", "DE")] // National format
    // Japan numbers
    [InlineData("+819012345678", "+819012345678")] // Mobile
    [InlineData("090-1234-5678", "+819012345678", "JP")] // National format
    // India numbers
    [InlineData("+919876543210", "+919876543210")] // Mobile
    [InlineData("098765 43210", "+919876543210", "IN")] // National format
    // With 00 prefix instead of +
    [InlineData("0061412345678", "+61412345678")] // Australian with 00
    [InlineData("00442071234567", "+442071234567")] // UK with 00
    public void PhoneNumber_WithValidValue_ShouldCreate(string input, string expected, string? countryCode = null)
    {
        // Act
        var phone = countryCode != null
            ? PhoneNumber.Create(input, countryCode)
            : PhoneNumber.Create(input);

        // Assert
        Assert.NotNull(phone);
        Assert.Equal(expected, phone.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void PhoneNumber_WithNullOrWhitespace_ShouldThrowArgumentException(string invalidPhone)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => PhoneNumber.Create(invalidPhone));
        Assert.Contains("phoneNumber", ex.Message);
    }

    [Theory]
    [InlineData("123456")] // Too short
    [InlineData("abc123")] // Contains only letters and numbers
    [InlineData("+123")] // Invalid country code
    [InlineData("+00000000000")] // Invalid number
    [InlineData("+999999999999")] // Non-existent country code
    // Removed - 0412345678 is valid with AU default
    [InlineData("555-2671")] // Too short US number without area code
    public void PhoneNumber_WithInvalidFormat_ShouldThrowArgumentException(string invalidPhone)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => PhoneNumber.Create(invalidPhone));
        Assert.Contains("Invalid phone number", ex.Message);
    }

    [Fact]
    public void PhoneNumber_Equality_ShouldWork()
    {
        // Arrange
        var phone1 = PhoneNumber.Create("+61412345678");
        var phone2 = PhoneNumber.Create("0412 345 678", "AU"); // Same number, different input
        var phone3 = PhoneNumber.Create("+61498765432");

        // Act & Assert
        Assert.Equal(phone1, phone2); // Same E164 representation
        Assert.NotEqual(phone1, phone3); // Different numbers
    }

    [Fact]
    public void PhoneNumber_ToString_ShouldReturnE164Value()
    {
        // Arrange
        var phone = PhoneNumber.Create("(415) 555-2671", "US");

        // Act
        var result = phone.ToString();

        // Assert
        Assert.Equal("+14155552671", result);
    }

    [Theory]
    // Test international formatting
    [InlineData("+61412345678", true, "+61 412 345 678")] // AU international
    [InlineData("+61412345678", false, "0412 345 678")] // AU national
    [InlineData("+14155552671", true, "+1 415-555-2671")] // US international
    [InlineData("+14155552671", false, "(415) 555-2671")] // US national
    [InlineData("+442071234567", true, "+44 20 7123 4567")] // UK international
    [InlineData("+442071234567", false, "020 7123 4567")] // UK national
    [InlineData("+5511987654321", true, "+55 11 98765-4321")] // BR international
    [InlineData("+5511987654321", false, "(11) 98765-4321")] // BR national
    public void PhoneNumber_GetFormatted_ShouldFormatCorrectly(string input, bool international, string expected)
    {
        // Arrange
        var phone = PhoneNumber.Create(input);

        // Act
        var formatted = phone.GetFormatted(international);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData("+61412345678", "AU")] // Australian mobile
    [InlineData("+14155552671", "US")] // US number
    [InlineData("+442071234567", "GB")] // UK number
    [InlineData("+5511987654321", "BR")] // Brazilian number
    [InlineData("+491701234567", "DE")] // German number
    [InlineData("+819012345678", "JP")] // Japanese number
    [InlineData("+919876543210", "IN")] // Indian number
    public void PhoneNumber_GetRegion_ShouldReturnCorrectRegion(string input, string expectedRegion)
    {
        // Arrange
        var phone = PhoneNumber.Create(input);

        // Act
        var region = phone.GetRegion();

        // Assert
        Assert.Equal(expectedRegion, region);
    }

    [Theory]
    [InlineData("+61412345678", true)] // Australian mobile
    [InlineData("+61298765432", false)] // Australian landline
    [InlineData("+14155552671", true)] // US number (555 prefix is actually mobile in this case)
    [InlineData("+447911123456", true)] // UK mobile
    [InlineData("+442071234567", false)] // UK landline
    [InlineData("+5511987654321", true)] // Brazilian mobile
    [InlineData("+491701234567", true)] // German mobile
    public void PhoneNumber_IsMobile_ShouldIdentifyMobileNumbers(string input, bool expectedIsMobile)
    {
        // Arrange
        var phone = PhoneNumber.Create(input);

        // Act
        var isMobile = phone.IsMobile();

        // Assert
        Assert.Equal(expectedIsMobile, isMobile);
    }

    [Fact]
    public void PhoneNumber_WithDifferentInputFormats_ShouldNormalizeToSameE164()
    {
        // Arrange - Different ways to write the same US number
        var inputs = new[]
        {
            "+14155552671",
            "001 415 555 2671", // With 00 prefix
            "(415) 555-2671", // National format with country hint
            "415-555-2671", // National format with country hint
            "4155552671", // National format with country hint
        };

        // Act - Create phone numbers
        var phone1 = PhoneNumber.Create(inputs[0]);
        var phone2 = PhoneNumber.Create(inputs[1]);
        var phone3 = PhoneNumber.Create(inputs[2], "US");
        var phone4 = PhoneNumber.Create(inputs[3], "US");
        var phone5 = PhoneNumber.Create(inputs[4], "US");

        // Assert - All should have the same E164 value
        Assert.Equal("+14155552671", phone1.Value);
        Assert.Equal("+14155552671", phone2.Value);
        Assert.Equal("+14155552671", phone3.Value);
        Assert.Equal("+14155552671", phone4.Value);
        Assert.Equal("+14155552671", phone5.Value);

        // All should be equal
        Assert.Equal(phone1, phone2);
        Assert.Equal(phone1, phone3);
        Assert.Equal(phone1, phone4);
        Assert.Equal(phone1, phone5);
    }

    [Fact]
    public void PhoneNumber_CountryCode_ShouldBePreserved()
    {
        // Arrange & Act
        var auPhone = PhoneNumber.Create("0412345678", "AU");
        var usPhone = PhoneNumber.Create("4155552671", "US");
        var ukPhone = PhoneNumber.Create("02071234567", "GB");

        // Assert
        Assert.Equal("AU", auPhone.CountryCode);
        Assert.Equal("US", usPhone.CountryCode);
        Assert.Equal("GB", ukPhone.CountryCode);
    }

    [Theory]
    [InlineData("+18005551234")] // US toll-free
    [InlineData("+448001234567")] // UK toll-free
    [InlineData("+611800123456")] // Australian toll-free
    public void PhoneNumber_TollFreeNumbers_ShouldBeValid(string tollFreeNumber)
    {
        // Act
        var phone = PhoneNumber.Create(tollFreeNumber);

        // Assert
        Assert.NotNull(phone);
        Assert.Equal(tollFreeNumber, phone.Value);
    }
}