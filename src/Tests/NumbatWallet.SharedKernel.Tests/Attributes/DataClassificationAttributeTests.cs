using Xunit;
using NumbatWallet.SharedKernel.Attributes;
using NumbatWallet.SharedKernel.Enums;
using System.Reflection;

namespace NumbatWallet.SharedKernel.Tests.Attributes;

public class DataClassificationAttributeTests
{
    private class TestEntity
    {
        [DataClassification(DataClassification.OfficialSensitive, "Identity")]
        public string FirstName { get; set; } = string.Empty;

        [DataClassification(DataClassification.Protected)]
        public string TaxFileNumber { get; set; } = string.Empty;

        public string UnclassifiedField { get; set; } = string.Empty;
    }

    [Fact]
    public void DataClassificationAttribute_Should_Store_Classification()
    {
        // Arrange
        var attribute = new DataClassificationAttribute(DataClassification.OfficialSensitive);

        // Assert
        Assert.Equal(DataClassification.OfficialSensitive, attribute.Classification);
        Assert.Null(attribute.Purpose);
    }

    [Fact]
    public void DataClassificationAttribute_Should_Store_Classification_And_Purpose()
    {
        // Arrange
        var attribute = new DataClassificationAttribute(
            DataClassification.Protected,
            "Financial");

        // Assert
        Assert.Equal(DataClassification.Protected, attribute.Classification);
        Assert.Equal("Financial", attribute.Purpose);
    }

    [Fact]
    public void Should_Be_Able_To_Retrieve_Attribute_From_Property()
    {
        // Arrange
        var property = typeof(TestEntity).GetProperty(nameof(TestEntity.FirstName));

        // Act
        var attribute = property?.GetCustomAttribute<DataClassificationAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(DataClassification.OfficialSensitive, attribute.Classification);
        Assert.Equal("Identity", attribute.Purpose);
    }

    [Fact]
    public void Should_Return_Null_For_Unclassified_Property()
    {
        // Arrange
        var property = typeof(TestEntity).GetProperty(nameof(TestEntity.UnclassifiedField));

        // Act
        var attribute = property?.GetCustomAttribute<DataClassificationAttribute>();

        // Assert
        Assert.Null(attribute);
    }

    [Fact]
    public void Should_Only_Be_Applicable_To_Properties()
    {
        // Arrange
        var attributeType = typeof(DataClassificationAttribute);

        // Act
        var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Property, usage.ValidOn);
    }
}