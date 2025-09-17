using Xunit;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Domain.Tests.ValueObjects;

public class EmailNullTests
{
    [Fact]
    public void Email_WithNull_ShouldFail()
    {
        // Act
        var email = Email.Create(null);

        // Assert
        Assert.True(email.IsFailure);
    }
}