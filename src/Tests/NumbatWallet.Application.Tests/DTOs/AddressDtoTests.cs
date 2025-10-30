using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Tests.DTOs;

public class AddressDtoTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateInstance()
    {
        // Arrange
        var street1 = "123 Main St";
        var street2 = "Apt 4B";
        var city = "Perth";
        var state = "WA";
        var postalCode = "6000";
        var country = "Australia";

        // Act
        var address = new AddressDto(street1, street2, city, state, postalCode, country);

        // Assert
        address.Street1.Should().Be(street1);
        address.Street2.Should().Be(street2);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.PostalCode.Should().Be(postalCode);
        address.Country.Should().Be(country);
    }

    [Fact]
    public void Constructor_WithNullStreet2_ShouldCreateInstance()
    {
        // Arrange & Act
        var address = new AddressDto(
            "123 Main St",
            null,
            "Perth",
            "WA",
            "6000",
            "Australia"
        );

        // Assert
        address.Street1.Should().Be("123 Main St");
        address.Street2.Should().BeNull();
        address.City.Should().Be("Perth");
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = new AddressDto("123 Main St", "Apt 4B", "Perth", "WA", "6000", "Australia");
        var address2 = new AddressDto("123 Main St", "Apt 4B", "Perth", "WA", "6000", "Australia");

        // Act & Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new AddressDto("123 Main St", null, "Perth", "WA", "6000", "Australia");
        var address2 = new AddressDto("456 Oak Ave", null, "Perth", "WA", "6000", "Australia");

        // Act & Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = new AddressDto("123 Main St", "Apt 4B", "Perth", "WA", "6000", "Australia");
        var address2 = new AddressDto("123 Main St", "Apt 4B", "Perth", "WA", "6000", "Australia");

        // Act & Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldContainAllProperties()
    {
        // Arrange
        var address = new AddressDto("123 Main St", "Apt 4B", "Perth", "WA", "6000", "Australia");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Contain("123 Main St");
        result.Should().Contain("Apt 4B");
        result.Should().Contain("Perth");
        result.Should().Contain("WA");
        result.Should().Contain("6000");
        result.Should().Contain("Australia");
    }

    [Fact]
    public void With_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = new AddressDto("123 Main St", null, "Perth", "WA", "6000", "Australia");

        // Act
        var modified = original with { Street2 = "Suite 100" };

        // Assert
        modified.Street1.Should().Be(original.Street1);
        modified.Street2.Should().Be("Suite 100");
        modified.City.Should().Be(original.City);
        original.Street2.Should().BeNull(); // Original unchanged
    }
}
