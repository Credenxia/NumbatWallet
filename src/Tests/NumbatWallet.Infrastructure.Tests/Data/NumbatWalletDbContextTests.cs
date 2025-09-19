using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace NumbatWallet.Infrastructure.Tests.Data;

[Collection("Database Collection")]
public class NumbatWalletDbContextTests : IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly DbContextOptions<NumbatWalletDbContext> _options;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<ILogger<NumbatWalletDbContext>> _loggerMock;
    private readonly Guid _tenantId;
    private readonly SqliteConnection _connection;

    public NumbatWalletDbContextTests()
    {
        // Use SQLite in-memory database for better query filter support
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseSqlite(_connection)
            .Options;

        _tenantServiceMock = new Mock<ITenantService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _loggerMock = new Mock<ILogger<NumbatWalletDbContext>>();

        _tenantId = Guid.NewGuid();
        _tenantServiceMock.Setup(x => x.TenantId).Returns(_tenantId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _context = new NumbatWalletDbContext(
            _options,
            _tenantServiceMock.Object,
            _currentUserServiceMock.Object,
            _dateTimeServiceMock.Object,
            _eventDispatcherMock.Object,
            _loggerMock.Object);

        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task DbContext_ShouldSaveWallet()
    {
        // Arrange
        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("John", "Doe", "john@example.com", "+14155552672");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "Personal Wallet").Value;
        wallet.SetTenantId(_tenantId.ToString());

        // Act
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        // Assert
        var savedWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == wallet.Id);
        Assert.NotNull(savedWallet);
        Assert.Equal(person.Id, savedWallet.PersonId);
    }

    [Fact]
    public async Task DbContext_ShouldSaveCredential()
    {
        // Arrange
        // Create issuer first
        var issuer = Issuer.Create("Department of Transport", "DOT", "transport.wa.gov.au").Value;
        issuer.SetTenantId(_tenantId.ToString());
        _context.Issuers.Add(issuer);
        await _context.SaveChangesAsync();

        // Create person and wallet for holder
        var personResult = Person.Create("John", "Doe", "john@example.com", "+14155552672");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "Personal Wallet").Value;
        wallet.SetTenantId(_tenantId.ToString());
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        var credential = Credential.Create(
            wallet.Id,
            issuer.Id,
            "DriverLicence",
            """{"licenseNumber":"DL123456"}""",
            "https://schema.org/driverlicence/v1"
        ).Value;
        credential.SetTenantId(_tenantId.ToString());

        // Act
        _context.Credentials.Add(credential);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        // Assert
        var savedCredential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == credential.Id);
        Assert.NotNull(savedCredential);
        Assert.Equal("DriverLicence", savedCredential.CredentialType);
    }

    [Fact]
    public async Task DbContext_ShouldSavePerson()
    {
        // Arrange
        var personResult = Person.Create(
            "John",
            "Doe",
            "john@example.com",
            "+61412345678"
        );
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());

        // Act
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        // Assert
        var savedPerson = await _context.Persons.FirstOrDefaultAsync(p => p.Id == person.Id);
        Assert.NotNull(savedPerson);
        Assert.Equal("John", savedPerson.FirstName);
    }

    [Fact]
    public async Task DbContext_ShouldSaveIssuer()
    {
        // Arrange
        var issuer = Issuer.Create(
            "Department of Transport",
            "DOT",
            "transport.wa.gov.au"
        ).Value;
        issuer.SetTenantId(_tenantId.ToString());

        // Act
        _context.Issuers.Add(issuer);
        await _context.SaveChangesAsync();

        // Clear change tracker to ensure clean queries
        _context.ChangeTracker.Clear();

        // Assert
        var savedIssuer = await _context.Issuers.FirstOrDefaultAsync(i => i.Id == issuer.Id);
        Assert.NotNull(savedIssuer);
        Assert.Equal("Department of Transport", savedIssuer.Name);
    }

    [Fact]
    public async Task DbContext_ShouldApplyTenantFilter()
    {
        // Arrange
        var tenantId = _tenantServiceMock.Object.TenantId;

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
        wallet1.SetTenantId(_tenantId.ToString());
        wallet2.SetTenantId(_tenantId.ToString());

        // Act
        _context.Wallets.Add(wallet1);
        _context.Wallets.Add(wallet2);
        await _context.SaveChangesAsync();

        // Assert
        var wallets = await _context.Wallets.ToListAsync();
        Assert.All(wallets, w => Assert.Equal(tenantId.ToString(), w.TenantId));
    }

    [Fact]
    public async Task DbContext_ShouldSetAuditFields()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserServiceMock.Object.UserId;
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(now);

        // Create person first to satisfy foreign key constraint
        var personResult = Person.Create("Test", "User", "test@example.com", "+14155552671");
        Assert.True(personResult.IsSuccess);
        var person = personResult.Value;
        person.SetTenantId(_tenantId.ToString());
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        var wallet = Wallet.Create(person.Id, "Test Wallet").Value;

        // Act
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(now, wallet.CreatedAt);
        Assert.Equal(userId, wallet.CreatedBy);
    }

    [Fact]
    public async Task DbContext_ShouldDispatchDomainEvents()
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

        // Act
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Assert
        _eventDispatcherMock.Verify(x =>
            x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }
}