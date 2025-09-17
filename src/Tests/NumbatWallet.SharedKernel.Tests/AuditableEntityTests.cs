using Xunit;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.SharedKernel.Tests;

public class AuditableEntityTests
{
    private class TestAuditableEntity : AuditableEntity<int>
    {
        public TestAuditableEntity(int id) : base(id) { }
    }

    [Fact]
    public void AuditableEntity_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(1);

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(default(DateTimeOffset), entity.CreatedAt);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.ModifiedAt);
        Assert.Null(entity.ModifiedBy);
    }

    [Fact]
    public void AuditableEntity_ShouldAllowSettingCreatedFields()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var createdAt = DateTimeOffset.UtcNow;
        var createdBy = "test-user";

        // Act
        entity.CreatedAt = createdAt;
        entity.CreatedBy = createdBy;

        // Assert
        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.Equal(createdBy, entity.CreatedBy);
    }

    [Fact]
    public void AuditableEntity_ShouldAllowSettingModifiedFields()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var modifiedAt = DateTimeOffset.UtcNow;
        var modifiedBy = "modified-user";

        // Act
        entity.ModifiedAt = modifiedAt;
        entity.ModifiedBy = modifiedBy;

        // Assert
        Assert.Equal(modifiedAt, entity.ModifiedAt);
        Assert.Equal(modifiedBy, entity.ModifiedBy);
    }

    [Fact]
    public void AuditableEntity_ShouldInheritEqualityFromEntity()
    {
        // Arrange
        var entity1 = new TestAuditableEntity(1);
        var entity2 = new TestAuditableEntity(1);
        var entity3 = new TestAuditableEntity(2);

        // Act & Assert
        Assert.Equal(entity1, entity2);
        Assert.NotEqual(entity1, entity3);
    }
}