using Xunit;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Repositories;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Specifications;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace NumbatWallet.Infrastructure.Tests.Repositories;

public class RepositoryBaseTests : IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly RepositoryBase<Wallet, Guid> _repository;
    private readonly Mock<ITenantService> _tenantServiceMock;

    public RepositoryBaseTests()
    {
        var options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _tenantServiceMock = new Mock<ITenantService>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        var eventDispatcherMock = new Mock<IEventDispatcher>();
        var loggerMock = new Mock<ILogger<NumbatWalletDbContext>>();

        _tenantServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");
        dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _context = new NumbatWalletDbContext(
            options,
            _tenantServiceMock.Object,
            currentUserServiceMock.Object,
            dateTimeServiceMock.Object,
            eventDispatcherMock.Object,
            loggerMock.Object);

        _repository = new RepositoryBase<Wallet, Guid>(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        // Arrange
        var wallet = Wallet.Create(Guid.NewGuid(), "Test Wallet").Value;
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(wallet.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(wallet.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        var wallet1 = Wallet.Create(Guid.NewGuid(), "Wallet 1").Value;
        var wallet2 = Wallet.Create(Guid.NewGuid(), "Wallet 2").Value;
        await _context.Wallets.AddRangeAsync(wallet1, wallet2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var wallet = Wallet.Create(Guid.NewGuid(), "New Wallet").Value;

        // Act
        await _repository.AddAsync(wallet);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Wallets.FindAsync(wallet.Id);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var wallet = Wallet.Create(Guid.NewGuid(), "Original Name").Value;
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        // Act
        wallet.UpdateName("Updated Name");
        await _repository.UpdateAsync(wallet);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Wallets.FindAsync(wallet.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        // Arrange
        var wallet = Wallet.Create(Guid.NewGuid(), "To Delete").Value;
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(wallet);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _context.Wallets.FindAsync(wallet.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsync_WithSpecification_ShouldReturnMatchingEntities()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var wallet1 = Wallet.Create(personId, "Wallet 1").Value;
        var wallet2 = Wallet.Create(Guid.NewGuid(), "Wallet 2").Value;
        await _context.Wallets.AddRangeAsync(wallet1, wallet2);
        await _context.SaveChangesAsync();

        var spec = new WalletByPersonIdSpecification(personId);

        // Act
        var results = await _repository.FindAsync(spec);

        // Assert
        Assert.Single(results);
        Assert.Equal(personId, results.First().PersonId);
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ShouldReturnCorrectCount()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var wallet1 = Wallet.Create(personId, "Wallet 1").Value;
        var wallet2 = Wallet.Create(personId, "Wallet 2").Value;
        var wallet3 = Wallet.Create(Guid.NewGuid(), "Wallet 3").Value;
        await _context.Wallets.AddRangeAsync(wallet1, wallet2, wallet3);
        await _context.SaveChangesAsync();

        var spec = new WalletByPersonIdSpecification(personId);

        // Act
        var count = await _repository.CountAsync(spec);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_ShouldReturnCorrectResult()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Wallet").Value;
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        var existingSpec = new WalletByPersonIdSpecification(personId);
        var nonExistingSpec = new WalletByPersonIdSpecification(Guid.NewGuid());

        // Act
        var exists = await _repository.AnyAsync(existingSpec);
        var notExists = await _repository.AnyAsync(nonExistingSpec);

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

public class WalletByPersonIdSpecification : BaseSpecification<Wallet>
{
    public WalletByPersonIdSpecification(Guid personId)
        : base(w => w.PersonId == personId)
    {
    }
}