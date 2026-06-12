using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Infrastructure.Crypto;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Converters;
using NumbatWallet.Infrastructure.Data.Interceptors;
using NumbatWallet.Infrastructure.Data.Repositories;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Tests.Data;

/// <summary>
/// End-to-end coverage of searchable PII encryption: SearchTokenInterceptor populates the
/// email/phone search-token shadow columns on save, the stored email/phone columns hold
/// ciphertext (not plaintext), and PersonRepository lookups resolve via the tokens.
/// </summary>
[Collection("Database Collection")]
public class SearchTokenInterceptorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NumbatWalletDbContext _context;
    private readonly HmacSearchTokenService _tokenService;
    private readonly Guid _tenantId;

    public SearchTokenInterceptorTests()
    {
        // Real AES-GCM encryptor so the test exercises the true at-rest shape.
        var encryptorConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FieldEncryption:Key"] = Convert.ToBase64String(Enumerable.Repeat((byte)3, 32).ToArray())
            })
            .Build();
        ProtectedFieldConverter.FieldEncryptor = new AesGcmFieldEncryptor(
            encryptorConfig,
            new Mock<ILogger<AesGcmFieldEncryptor>>().Object);

        var tokenConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Search:TokenPepper"] = Convert.ToBase64String(Enumerable.Repeat((byte)9, 32).ToArray())
            })
            .Build();
        _tokenService = new HmacSearchTokenService(
            new Mock<IKeyVaultService>().Object,
            new Mock<ICurrentTenantService>().Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<HmacSearchTokenService>>().Object,
            tokenConfig);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NumbatWalletDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new SearchTokenInterceptor(_tokenService))
            .Options;

        _tenantId = Guid.NewGuid();
        var tenantServiceMock = new Mock<SharedKernel.Interfaces.ITenantService>();
        tenantServiceMock.Setup(x => x.TenantId).Returns(_tenantId);
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _context = new NumbatWalletDbContext(
            options,
            tenantServiceMock.Object,
            currentUserServiceMock.Object,
            dateTimeServiceMock.Object,
            new Mock<SharedKernel.Interfaces.IEventDispatcher>().Object,
            new Mock<ILogger<NumbatWalletDbContext>>().Object);

        _context.Database.EnsureCreated();
    }

    private Person CreatePerson(string email = "citizen@example.com", string phone = "+61400000000")
    {
        var person = Person.Create("Test", "Citizen", email, phone).Value;
        person.SetTenantId(_tenantId.ToString());
        return person;
    }

    [Fact]
    public async Task SaveChanges_OnAdd_PopulatesEmailAndPhoneSearchTokens()
    {
        // Arrange
        var person = CreatePerson();
        _context.Persons.Add(person);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        var entry = _context.Entry(person);
        var emailToken = entry.Property<string?>("EmailSearchToken").CurrentValue;
        var phoneToken = entry.Property<string?>("PhoneSearchToken").CurrentValue;

        emailToken.Should().Be(await _tokenService.GenerateEmailTokenAsync("citizen@example.com"));
        phoneToken.Should().Be(await _tokenService.GeneratePhoneTokenAsync("+61400000000"));
    }

    [Fact]
    public async Task SaveChanges_StoresCiphertextNotPlaintextEmail()
    {
        // Arrange
        var person = CreatePerson();
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        // Act - read the raw column value
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT email FROM persons LIMIT 1";
        var rawEmail = (string)(await command.ExecuteScalarAsync())!;

        // Assert - ciphertext JSON, no plaintext anywhere in the stored value
        rawEmail.Should().NotContain("citizen@example.com");
        rawEmail.Should().Contain("FE1:"); // AES-GCM token prefix
        rawEmail.Should().Contain("AES-256-GCM");
    }

    [Fact]
    public async Task PersonRepository_GetByEmailAsync_FindsPersonViaSearchToken()
    {
        // Arrange
        var person = CreatePerson();
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var repository = new PersonRepository(_context, _tokenService);

        // Act - lookup uses a different casing to prove normalization
        var found = await repository.GetByEmailAsync("Citizen@Example.com");

        // Assert - found via token, and the decrypted value round-trips
        found.Should().NotBeNull();
        found!.Id.Should().Be(person.Id);
        found.Email.Value.Should().Be("citizen@example.com");
    }

    [Fact]
    public async Task PersonRepository_GetByMobileNumberAsync_FindsPersonViaSearchToken()
    {
        // Arrange
        var person = CreatePerson();
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var repository = new PersonRepository(_context, _tokenService);

        // Act - formatted variant of the same number
        var found = await repository.GetByMobileNumberAsync("+61 400 000 000");

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(person.Id);
    }

    [Fact]
    public async Task PersonRepository_GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        // Arrange
        _context.Persons.Add(CreatePerson());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var repository = new PersonRepository(_context, _tokenService);

        // Act / Assert
        (await repository.GetByEmailAsync("other@example.com")).Should().BeNull();
    }

    [Fact]
    public async Task SaveChanges_OnPhoneUpdate_RecomputesPhoneToken()
    {
        // Arrange
        var person = CreatePerson();
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();

        // Act - replace the owned value object; owner entry may stay Unchanged
        person.UpdatePhoneNumber(Domain.ValueObjects.PhoneNumber.Create("+61499999999"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var repository = new PersonRepository(_context, _tokenService);

        // Assert - findable by the new number, not the old one
        (await repository.GetByMobileNumberAsync("+61499999999")).Should().NotBeNull();
        (await repository.GetByMobileNumberAsync("+61400000000")).Should().BeNull();
    }

    [Fact]
    public async Task PersonRepository_IsEmailUniqueAsync_DetectsExistingEmailViaToken()
    {
        // Arrange
        _context.Persons.Add(CreatePerson());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var repository = new PersonRepository(_context, _tokenService);

        // Act / Assert
        (await repository.IsEmailUniqueAsync("citizen@example.com")).Should().BeFalse();
        (await repository.IsEmailUniqueAsync("new@example.com")).Should().BeTrue();
    }

    public void Dispose()
    {
        ProtectedFieldConverter.FieldEncryptor = null;
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
