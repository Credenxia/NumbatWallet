using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.SharedKernel.Tests;

public class EntityTests
{
    private class TestEntity : Entity<int>
    {
        public TestEntity(int id) : base(id) { }
    }

    private class TestGuidEntity : Entity<Guid>
    {
        public TestGuidEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void Entity_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = 1;
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        Assert.Equal(entity1, entity2);
        Assert.True(entity1 == entity2);
        Assert.False(entity1 != entity2);
    }

    [Fact]
    public void Entity_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        // Act & Assert
        Assert.NotEqual(entity1, entity2);
        Assert.False(entity1 == entity2);
        Assert.True(entity1 != entity2);
    }

    [Fact]
    public void Entity_GetHashCode_ShouldBeSameForSameId()
    {
        // Arrange
        var id = 1;
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    public void Entity_Id_ShouldBeAccessible()
    {
        // Arrange
        var id = 42;
        var entity = new TestEntity(id);

        // Act & Assert
        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Entity_WithGuidId_ShouldWorkCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestGuidEntity(id);
        var entity2 = new TestGuidEntity(id);
        var entity3 = new TestGuidEntity(Guid.NewGuid());

        // Act & Assert
        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, entity3);
    }

    [Fact]
    public void Entity_ComparedWithNull_ShouldNotBeEqual()
    {
        // Arrange
        var entity = new TestEntity(1);

        // Act & Assert
        Assert.False(entity == null);
        Assert.False(entity.Equals(null));
        Assert.True(entity != null);
    }
}