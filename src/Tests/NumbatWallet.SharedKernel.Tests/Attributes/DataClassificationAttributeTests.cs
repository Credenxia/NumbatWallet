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
    public void DataClassificationAttribute_Unofficial_ShouldNotRequireEncryption()
    {
        // Arrange & Act
        var attribute = new DataClassificationAttribute(DataClassification.Unofficial);

        // Assert
        Assert.False(attribute.RequiresEncryption);
        Assert.False(attribute.RequiresAudit);
        Assert.Equal(365, attribute.RetentionDays);
    }

    [Fact]
    public void DataClassificationAttribute_Official_ShouldRequireAuditOnly()
    {
        // Arrange & Act
        var attribute = new DataClassificationAttribute(DataClassification.Official);

        // Assert
        Assert.False(attribute.RequiresEncryption);
        Assert.True(attribute.RequiresAudit);
        Assert.Equal(365 * 3, attribute.RetentionDays);
    }

    [Fact]
    public void DataClassificationAttribute_OfficialSensitive_ShouldRequireEncryptionAndAudit()
    {
        // Arrange & Act
        var attribute = new DataClassificationAttribute(DataClassification.OfficialSensitive, "PII");

        // Assert
        Assert.True(attribute.RequiresEncryption);
        Assert.True(attribute.RequiresAudit);
        Assert.Equal(365 * 7, attribute.RetentionDays);
    }

    [Fact]
    public void DataClassificationAttribute_Protected_ShouldHaveStrongestProtection()
    {
        // Arrange & Act
        var attribute = new DataClassificationAttribute(DataClassification.Protected);

        // Assert
        Assert.True(attribute.RequiresEncryption);
        Assert.True(attribute.RequiresAudit);
        Assert.Equal(365 * 25, attribute.RetentionDays);
    }

    [Fact]
    public void DataClassificationAttribute_Secret_ShouldHaveIndefiniteRetention()
    {
        // Arrange & Act
        var attribute = new DataClassificationAttribute(DataClassification.Secret);

        // Assert
        Assert.True(attribute.RequiresEncryption);
        Assert.True(attribute.RequiresAudit);
        Assert.Equal(-1, attribute.RetentionDays); // Indefinite retention
    }

    [Theory]
    [InlineData(DataClassification.Unofficial, false, false, 365)]
    [InlineData(DataClassification.Official, false, true, 365 * 3)]
    [InlineData(DataClassification.OfficialSensitive, true, true, 365 * 7)]
    [InlineData(DataClassification.Protected, true, true, 365 * 25)]
    [InlineData(DataClassification.Secret, true, true, -1)]
    public void DataClassificationAttribute_AutoConfiguration_ShouldMatchClassification(
        DataClassification classification,
        bool expectedEncryption,
        bool expectedAudit,
        int expectedRetention)
    {
        // Arrange & Act
        var attribute = new DataClassificationAttribute(classification);

        // Assert
        Assert.Equal(expectedEncryption, attribute.RequiresEncryption);
        Assert.Equal(expectedAudit, attribute.RequiresAudit);
        Assert.Equal(expectedRetention, attribute.RetentionDays);
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
        Assert.Equal(AttributeTargets.Property | AttributeTargets.Class, usage.ValidOn);
    }
}

public class HandlingCaveatsAttributeTests
{
    [DataClassification(DataClassification.OfficialSensitive)]
    [HandlingCaveats("FOR-OFFICIAL-USE-ONLY", "CABINET-IN-CONFIDENCE")]
    private class ClassifiedDocument
    {
        [DataClassification(DataClassification.Protected)]
        [HandlingCaveats("LEGAL-PROFESSIONAL-PRIVILEGE")]
        public string Content { get; set; } = string.Empty;

        public string PublicInformation { get; set; } = string.Empty;
    }

    [Fact]
    public void HandlingCaveatsAttribute_Should_Store_SingleCaveat()
    {
        // Arrange & Act
        var attribute = new HandlingCaveatsAttribute("FOR-OFFICIAL-USE-ONLY");

        // Assert
        Assert.NotNull(attribute.Caveats);
        Assert.Single(attribute.Caveats);
        Assert.Equal("FOR-OFFICIAL-USE-ONLY", attribute.Caveats[0]);
    }

    [Fact]
    public void HandlingCaveatsAttribute_Should_Store_MultipleCaveats()
    {
        // Arrange & Act
        var attribute = new HandlingCaveatsAttribute("FOR-OFFICIAL-USE-ONLY", "CABINET-IN-CONFIDENCE", "LEGAL-PROFESSIONAL-PRIVILEGE");

        // Assert
        Assert.NotNull(attribute.Caveats);
        Assert.Equal(3, attribute.Caveats.Length);
        Assert.Contains("FOR-OFFICIAL-USE-ONLY", attribute.Caveats);
        Assert.Contains("CABINET-IN-CONFIDENCE", attribute.Caveats);
        Assert.Contains("LEGAL-PROFESSIONAL-PRIVILEGE", attribute.Caveats);
    }

    [Fact]
    public void HandlingCaveatsAttribute_Should_HandleNullCaveats()
    {
        // Arrange & Act
        var attribute = new HandlingCaveatsAttribute(null!);

        // Assert
        Assert.NotNull(attribute.Caveats);
        Assert.Empty(attribute.Caveats);
    }

    [Fact]
    public void HandlingCaveatsAttribute_Should_HandleEmptyArray()
    {
        // Arrange & Act
        var attribute = new HandlingCaveatsAttribute();

        // Assert
        Assert.NotNull(attribute.Caveats);
        Assert.Empty(attribute.Caveats);
    }

    [Fact]
    public void Should_Be_Able_To_Retrieve_HandlingCaveats_From_Class()
    {
        // Arrange
        var classType = typeof(ClassifiedDocument);

        // Act
        var attribute = classType.GetCustomAttribute<HandlingCaveatsAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(2, attribute.Caveats.Length);
        Assert.Contains("FOR-OFFICIAL-USE-ONLY", attribute.Caveats);
        Assert.Contains("CABINET-IN-CONFIDENCE", attribute.Caveats);
    }

    [Fact]
    public void Should_Be_Able_To_Retrieve_HandlingCaveats_From_Property()
    {
        // Arrange
        var property = typeof(ClassifiedDocument).GetProperty(nameof(ClassifiedDocument.Content));

        // Act
        var attribute = property?.GetCustomAttribute<HandlingCaveatsAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Single(attribute.Caveats);
        Assert.Equal("LEGAL-PROFESSIONAL-PRIVILEGE", attribute.Caveats[0]);
    }

    [Fact]
    public void Should_Return_Null_For_Property_Without_HandlingCaveats()
    {
        // Arrange
        var property = typeof(ClassifiedDocument).GetProperty(nameof(ClassifiedDocument.PublicInformation));

        // Act
        var attribute = property?.GetCustomAttribute<HandlingCaveatsAttribute>();

        // Assert
        Assert.Null(attribute);
    }

    [Fact]
    public void HandlingCaveatsAttribute_Should_Only_Be_Applicable_To_Properties_And_Classes()
    {
        // Arrange
        var attributeType = typeof(HandlingCaveatsAttribute);

        // Act
        var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Property | AttributeTargets.Class, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
    }

    [Fact]
    public void Should_Work_With_DataClassification_On_Same_Element()
    {
        // Arrange
        var property = typeof(ClassifiedDocument).GetProperty(nameof(ClassifiedDocument.Content));

        // Act
        var classificationAttr = property?.GetCustomAttribute<DataClassificationAttribute>();
        var caveatsAttr = property?.GetCustomAttribute<HandlingCaveatsAttribute>();

        // Assert
        Assert.NotNull(classificationAttr);
        Assert.NotNull(caveatsAttr);
        Assert.Equal(DataClassification.Protected, classificationAttr.Classification);
        Assert.Equal("LEGAL-PROFESSIONAL-PRIVILEGE", caveatsAttr.Caveats[0]);
    }
}