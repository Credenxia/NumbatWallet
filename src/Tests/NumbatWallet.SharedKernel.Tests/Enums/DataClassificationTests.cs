using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.SharedKernel.Tests.Enums;

public class DataClassificationTests
{
    [Fact]
    public void DataClassification_Should_Have_Correct_Values()
    {
        // Assert
        Assert.Equal(0, (int)DataClassification.Unofficial);
        Assert.Equal(1, (int)DataClassification.Official);
        Assert.Equal(2, (int)DataClassification.OfficialSensitive);
        Assert.Equal(3, (int)DataClassification.Protected);
        Assert.Equal(4, (int)DataClassification.Secret);
        Assert.Equal(5, (int)DataClassification.TopSecret);
    }

    [Theory]
    [InlineData(DataClassification.OfficialSensitive, DataClassification.Official)]
    [InlineData(DataClassification.Protected, DataClassification.OfficialSensitive)]
    [InlineData(DataClassification.Secret, DataClassification.Protected)]
    public void Higher_Classification_Should_Be_More_Restrictive(
        DataClassification higher,
        DataClassification lower)
    {
        // Assert
        Assert.True((int)higher > (int)lower);
    }

    [Fact]
    public void Should_Be_Able_To_Compare_Classifications()
    {
        // Arrange
        var sensitive = DataClassification.OfficialSensitive;
        var protected_ = DataClassification.Protected;

        // Act & Assert
        Assert.True(protected_ > sensitive);
        Assert.False(sensitive > protected_);
        Assert.True(sensitive >= DataClassification.Official);
    }
}