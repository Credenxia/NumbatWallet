using Xunit;
using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Domain.Tests.ValueObjects;

public class PhoneNumberNullTests
{
    [Fact]
    public void PhoneNumber_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(null!));
    }

    [Fact]
    public void PhoneNumber_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(string.Empty));
    }
}