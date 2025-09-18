using Xunit;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("john.doe+test@company.co.uk")]
    [InlineData("admin@sub.domain.org")]
    public void Email_WithValidValue_ShouldCreate(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        Assert.NotNull(email);
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Email_WithNullOrWhitespace_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    [InlineData("user@.com")]
    public void Email_WithInvalidFormat_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        Assert.Contains("Invalid email format", ex.Message);
    }

    [Fact]
    public void Email_Equality_ShouldBeCaseInsensitive()
    {
        // Arrange
        var email1 = Email.Create("User@Example.Com");
        var email2 = Email.Create("user@example.com");

        // Act & Assert
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void Email_ToString_ShouldReturnValue()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act & Assert
        Assert.Equal("user@example.com", email.ToString());
    }

    [Fact]
    public void Email_Domain_ShouldReturnDomainPart()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act & Assert
        Assert.Equal("example.com", email.Domain);
    }

    [Fact]
    public void Email_LocalPart_ShouldReturnLocalPart()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act & Assert
        Assert.Equal("user", email.LocalPart);
    }

    [Fact]
    public void Email_ImplicitOperator_ShouldConvertToString()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act
        string emailString = email;

        // Assert
        Assert.Equal("user@example.com", emailString);
    }

    [Fact]
    public void Email_WithUpperCase_ShouldNormalizeToLowerCase()
    {
        // Arrange & Act
        var email = Email.Create("User@EXAMPLE.COM");

        // Assert
        Assert.Equal("user@example.com", email.Value);
    }
}