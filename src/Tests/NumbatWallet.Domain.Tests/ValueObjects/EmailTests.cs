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
        Assert.True(email.IsSuccess);
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    [InlineData("user@.com")]
    public void Email_WithInvalidValue_ShouldFail(string invalidEmail)
    {
        // Act
        var email = Email.Create(invalidEmail);

        // Assert
        Assert.True(email.IsFailure);
    }

    [Fact]
    public void Email_Equality_ShouldBeCaseInsensitive()
    {
        // Arrange
        var email1 = Email.Create("User@Example.Com").Value;
        var email2 = Email.Create("user@example.com").Value;

        // Act & Assert
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void Email_ToString_ShouldReturnValue()
    {
        // Arrange
        var email = Email.Create("user@example.com").Value;

        // Act & Assert
        Assert.Equal("user@example.com", email.ToString());
    }
}