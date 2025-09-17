using Xunit;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Domain.Tests.ValueObjects;

public class PhoneNumberNullTests
{
    [Fact]
    public void PhoneNumber_WithNull_ShouldFail()
    {
        // Act
        var phone = PhoneNumber.Create(null);

        // Assert
        Assert.True(phone.IsFailure);
    }
}