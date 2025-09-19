using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Repositories;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Specifications;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace NumbatWallet.Infrastructure.Tests.Repositories;

[Collection("Database Collection")]
public class RepositoryBaseTests : IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly RepositoryBase<Wallet, Guid> _repository;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Guid _tenantId;
    private readonly SqliteConnection _connection;

    public RepositoryBaseTests()
    {
        // Use SQLite in-memory database for better query filter support
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        _tenantServiceMock = new Mock<ITenantService>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        var eventDispatcherMock = new Mock<IEventDispatcher>();
        var loggerMock = new Mock<ILogger<NumbatWalletDbContext>>();

        _tenantId = Guid.NewGuid();
        _tenantServiceMock.Setup(x => x.TenantId).Returns(_tenantId);
        currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");
        dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _context = new NumbatWalletDbContext(
            options,
            _tenantServiceMock.Object,
            currentUserServiceMock.Object,
            dateTimeServiceMock.Object,
            eventDispatcherMock.Object,
            loggerMock.Object);

        _context.Database.EnsureCreated();

        _repository = new RepositoryBase<Wallet, Guid>(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        // Arrange
        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("Test", "User", "test@example.com", "+14155552671");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "Test Wallet").Value;
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
        // Create persons first to satisfy foreign key constraints
        var person1Result = Person.Create("John", "Doe", "john@example.com", "+14155552672");
        var person2Result = Person.Create("Jane", "Smith", "jane@example.com", "+442071234567");
        Assert.True(person1Result.IsSuccess);
        Assert.True(person2Result.IsSuccess);
        var person1 = person1Result.Value;
        var person2 = person2Result.Value;
        person1.SetTenantId(_tenantId.ToString());
        person2.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person1);
        _context.Persons.Add(person2);
        await _context.SaveChangesAsync();

        var wallet1 = Wallet.Create(person1.Id, "Wallet 1").Value;
        var wallet2 = Wallet.Create(person2.Id, "Wallet 2").Value;

        // Use repository to add entities (it will handle TenantId)
        await _repository.AddAsync(wallet1);
        await _repository.AddAsync(wallet2);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("Test", "User", "test@example.com", "+14155552671");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "New Wallet").Value;

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
        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("Test", "User", "test@example.com", "+14155552671");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "Original Name").Value;
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
        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("Test", "User", "test@example.com", "+14155552671");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "To Delete").Value;
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
        // Create persons first to satisfy foreign key constraints
        var person1Result = Person.Create("John", "Doe", "john@example.com", "+14155552672");
        var person2Result = Person.Create("Jane", "Smith", "jane@example.com", "+442071234567");
        Assert.True(person1Result.IsSuccess);
        Assert.True(person2Result.IsSuccess);
        var person1 = person1Result.Value;
        var person2 = person2Result.Value;
        person1.SetTenantId(_tenantId.ToString());
        person2.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person1);
        _context.Persons.Add(person2);
        await _context.SaveChangesAsync();

        var wallet1 = Wallet.Create(person1.Id, "Wallet 1").Value;
        var wallet2 = Wallet.Create(person2.Id, "Wallet 2").Value;

        // Use repository to add entities
        await _repository.AddAsync(wallet1);
        await _repository.AddAsync(wallet2);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        var spec = new WalletByPersonIdSpecification(person1.Id);

        // Act
        var results = await _repository.FindAsync(spec);

        // Assert
        Assert.Single(results);
        Assert.Equal(person1.Id, results.First().PersonId);
    }

    [Fact]
    public async Task CountAsync_WithSpecification_ShouldReturnCorrectCount()
    {
        // Arrange
        // Create persons first to satisfy foreign key constraints
        var person1Result = Person.Create("John", "Doe", "john@example.com", "+14155552672");
        var person2Result = Person.Create("Jane", "Smith", "jane@example.com", "+442071234567");
        Assert.True(person1Result.IsSuccess);
        Assert.True(person2Result.IsSuccess);
        var person1 = person1Result.Value;
        var person2 = person2Result.Value;
        person1.SetTenantId(_tenantId.ToString());
        person2.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person1);
        _context.Persons.Add(person2);
        await _context.SaveChangesAsync();

        var wallet1 = Wallet.Create(person1.Id, "Wallet 1").Value;
        var wallet2 = Wallet.Create(person1.Id, "Wallet 2").Value;
        var wallet3 = Wallet.Create(person2.Id, "Wallet 3").Value;

        // Use repository to add entities
        await _repository.AddAsync(wallet1);
        await _repository.AddAsync(wallet2);
        await _repository.AddAsync(wallet3);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        var spec = new WalletByPersonIdSpecification(person1.Id);

        // Act
        var count = await _repository.CountAsync(spec);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_ShouldReturnCorrectResult()
    {
        // Arrange
        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("Test", "User", "test@example.com", "+14155552671");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "Wallet").Value;

        // Use repository to add entity
        await _repository.AddAsync(wallet);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        var existingSpec = new WalletByPersonIdSpecification(person.Id);
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
        _connection?.Dispose();
    }
}

public class WalletByPersonIdSpecification : BaseSpecification<Wallet>
{
    public WalletByPersonIdSpecification(Guid personId)
        : base(w => w.PersonId == personId)
    {
    }
}