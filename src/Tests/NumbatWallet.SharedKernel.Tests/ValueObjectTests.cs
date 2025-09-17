using Xunit;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.SharedKernel.Tests;

public class ValueObjectTests
{
    private class TestValueObject : ValueObject
    {
        public string Value1 { get; }
        public int Value2 { get; }

        public TestValueObject(string value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value1;
            yield return Value2;
        }
    }

    private class SingleValueObject : ValueObject
    {
        public string Value { get; }

        public SingleValueObject(string value)
        {
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    [Fact]
    public void ValueObject_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("test", 42);

        // Act & Assert
        Assert.Equal(vo1, vo2);
        Assert.True(vo1 == vo2);
        Assert.False(vo1 != vo2);
    }

    [Fact]
    public void ValueObject_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("test", 43);
        var vo3 = new TestValueObject("different", 42);

        // Act & Assert
        Assert.NotEqual(vo1, vo2);
        Assert.NotEqual(vo1, vo3);
        Assert.True(vo1 != vo2);
        Assert.True(vo1 != vo3);
    }

    [Fact]
    public void ValueObject_GetHashCode_ShouldBeSameForEqualObjects()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("test", 42);

        // Act & Assert
        Assert.Equal(vo1.GetHashCode(), vo2.GetHashCode());
    }

    [Fact]
    public void ValueObject_GetHashCode_ShouldBeDifferentForDifferentObjects()
    {
        // Arrange
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("test", 43);

        // Act & Assert
        Assert.NotEqual(vo1.GetHashCode(), vo2.GetHashCode());
    }

    [Fact]
    public void ValueObject_ComparedWithNull_ShouldNotBeEqual()
    {
        // Arrange
        var vo = new TestValueObject("test", 42);

        // Act & Assert
        Assert.False(vo == null);
        Assert.False(vo.Equals(null));
        Assert.True(vo != null);
    }

    [Fact]
    public void ValueObject_NullValues_ShouldBeHandledCorrectly()
    {
        // Arrange
        var vo1 = new TestValueObject(null!, 42);
        var vo2 = new TestValueObject(null!, 42);
        var vo3 = new TestValueObject("test", 42);

        // Act & Assert
        Assert.Equal(vo1, vo2);
        Assert.NotEqual(vo1, vo3);
    }

    [Fact]
    public void ValueObject_Copy_ShouldCreateEqualObject()
    {
        // Arrange
        var original = new TestValueObject("test", 42);

        // Act
        var copy = original.Copy();

        // Assert
        Assert.Equal(original, copy);
        Assert.NotSame(original, copy);
    }
}