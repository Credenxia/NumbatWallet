using Xunit;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace NumbatWallet.Infrastructure.Tests.Data;

public class NumbatWalletDbContextTests : IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly DbContextOptions<NumbatWalletDbContext> _options;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<IEventDispatcher> _eventDispatcherMock;
    private readonly Mock<ILogger<NumbatWalletDbContext>> _loggerMock;

    public NumbatWalletDbContextTests()
    {
        _options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _tenantServiceMock = new Mock<ITenantService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _eventDispatcherMock = new Mock<IEventDispatcher>();
        _loggerMock = new Mock<ILogger<NumbatWalletDbContext>>();

        _tenantServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _context = new NumbatWalletDbContext(
            _options,
            _tenantServiceMock.Object,
            _currentUserServiceMock.Object,
            _dateTimeServiceMock.Object,
            _eventDispatcherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DbContext_ShouldSaveWallet()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Personal Wallet").Value;

        // Act
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Assert
        var savedWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == wallet.Id);
        Assert.NotNull(savedWallet);
        Assert.Equal(personId, savedWallet.PersonId);
    }

    [Fact]
    public async Task DbContext_ShouldSaveCredential()
    {
        // Arrange
        var credential = Credential.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DriverLicence",
            """{"licenseNumber":"DL123456"}""",
            "https://schema.org/driverlicence/v1"
        ).Value;

        // Act
        _context.Credentials.Add(credential);
        await _context.SaveChangesAsync();

        // Assert
        var savedCredential = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == credential.Id);
        Assert.NotNull(savedCredential);
        Assert.Equal("DriverLicence", savedCredential.CredentialType);
    }

    [Fact]
    public async Task DbContext_ShouldSavePerson()
    {
        // Arrange
        var person = Person.Create(
            "John",
            "Doe",
            "john@example.com",
            "+61412345678"
        ).Value;

        // Act
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

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

        // Act
        _context.Issuers.Add(issuer);
        await _context.SaveChangesAsync();

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
        var personId1 = Guid.NewGuid();
        var personId2 = Guid.NewGuid();
        var wallet1 = Wallet.Create(personId1, "Wallet 1").Value;
        var wallet2 = Wallet.Create(personId2, "Wallet 2").Value;

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

        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;

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
        var personId = Guid.NewGuid();
        var wallet = Wallet.Create(personId, "Test Wallet").Value;

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
    }
}