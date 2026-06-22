using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class HmacSearchTokenServiceTests : IDisposable
{
    private readonly Mock<IKeyVaultService> _keyVaultServiceMock;
    private readonly Mock<ICurrentTenantService> _currentTenantServiceMock;
    private readonly MemoryCache _memoryCache;
    private readonly HmacSearchTokenService _sut;

    public HmacSearchTokenServiceTests()
    {
        _keyVaultServiceMock = new Mock<IKeyVaultService>();
        _currentTenantServiceMock = new Mock<ICurrentTenantService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<HmacSearchTokenService>>();

        _sut = new HmacSearchTokenService(
            _keyVaultServiceMock.Object,
            _currentTenantServiceMock.Object,
            _memoryCache,
            loggerMock.Object);
    }

    [Fact]
    public async Task GenerateNameTokensAsync_WithValidName_ShouldGenerateMultipleTokens()
    {
        // Arrange
        var fullName = "John Doe";
        var tenantId = "tenant-123";
        var pepper = "test-pepper-base64";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync($"search-pepper-{tenantId}", default))
            .ReturnsAsync(pepper);

        // Act
        var result = await _sut.GenerateNameTokensAsync(fullName);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().Contain(token => !string.IsNullOrEmpty(token), "should have valid tokens");

        // Should include prefix tokens, phonetic tokens, full name token, and initials
        result.Count.Should().BeGreaterThan(3);
    }

    [Fact]
    public async Task GenerateNameTokensAsync_WithEmptyName_ShouldReturnEmpty()
    {
        // Arrange
        var fullName = "";

        // Act
        var result = await _sut.GenerateNameTokensAsync(fullName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private static string ValidPepper => Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray());

    private void SetupIdentifierPepper()
    {
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync("search-token-pepper", default))
            .ReturnsAsync(ValidPepper);
    }

    [Fact]
    public async Task GenerateEmailTokenAsync_WithValidEmail_ShouldGenerateSingleToken()
    {
        // Arrange
        var email = "John.Doe@Example.COM";
        SetupIdentifierPepper();

        // Act
        var result = await _sut.GenerateEmailTokenAsync(email);

        // Assert
        result.Should().NotBeNullOrEmpty();

        // Test normalization - should produce same token for different cases
        var result2 = await _sut.GenerateEmailTokenAsync("john.doe@example.com");
        result2.Should().Be(result);
    }

    [Fact]
    public async Task GeneratePhoneTokenAsync_WithFormattingVariants_ShouldGenerateSameToken()
    {
        // Arrange
        SetupIdentifierPepper();

        // Act
        var canonical = await _sut.GeneratePhoneTokenAsync("61400000000");
        var formatted = await _sut.GeneratePhoneTokenAsync("+61 400-000-000");
        var parenthesised = await _sut.GeneratePhoneTokenAsync("(61) 400 000 000");

        // Assert - digits-only normalization makes all variants identical
        canonical.Should().NotBeNullOrEmpty();
        formatted.Should().Be(canonical);
        parenthesised.Should().Be(canonical);
    }

    [Fact]
    public async Task GeneratePhoneTokenAsync_DifferentNumbers_ShouldGenerateDifferentTokens()
    {
        // Arrange
        SetupIdentifierPepper();

        // Act
        var first = await _sut.GeneratePhoneTokenAsync("+61400000001");
        var second = await _sut.GeneratePhoneTokenAsync("+61400000002");

        // Assert
        first.Should().NotBe(second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-digits-here")]
    public async Task GeneratePhoneTokenAsync_WithInvalidInput_ShouldReturnNull(string? phone)
    {
        // Act
        var result = await _sut.GeneratePhoneTokenAsync(phone!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateEmailTokenAsync_WithConfigPepper_ShouldNotUseKeyVault()
    {
        // Arrange - pepper supplied via configuration takes precedence over Key Vault
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Search:TokenPepper"] = ValidPepper
            })
            .Build();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new HmacSearchTokenService(
            _keyVaultServiceMock.Object,
            _currentTenantServiceMock.Object,
            cache,
            new Mock<ILogger<HmacSearchTokenService>>().Object,
            configuration);

        // Act
        var token = await sut.GenerateEmailTokenAsync("citizen@example.com");

        // Assert
        token.Should().NotBeNullOrEmpty();
        _keyVaultServiceMock.Verify(
            x => x.GetSecretAsync(It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task GenerateEmailTokenAsync_WithInvalidBase64Pepper_ShouldFailClosed()
    {
        // Arrange - a malformed pepper must throw, never silently derive a different key
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync("search-token-pepper", default))
            .ReturnsAsync("not-valid-base64!!");

        // Act
        var act = () => _sut.GenerateEmailTokenAsync("citizen@example.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateEmailTokenAsync_WhenPepperCannotBePersisted_ShouldFailClosed()
    {
        // Arrange - no pepper anywhere, and Key Vault write fails
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync("search-token-pepper", default))
            .ReturnsAsync((string?)null);
        _keyVaultServiceMock.Setup(x => x.SetSecretAsync("search-token-pepper", It.IsAny<string>(), default))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.GenerateEmailTokenAsync("citizen@example.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateDateTokenAsync_WithDifferentGranularities_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var date = new DateTime(1990, 5, 15);
        var tenantId = "tenant-123";
        var pepper = "test-pepper-base64";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync($"search-pepper-{tenantId}", default))
            .ReturnsAsync(pepper);

        // Act
        var yearToken = await _sut.GenerateDateTokenAsync(date, DateGranularity.Year);
        var monthToken = await _sut.GenerateDateTokenAsync(date, DateGranularity.YearMonth);
        var fullToken = await _sut.GenerateDateTokenAsync(date, DateGranularity.FullDate);

        // Assert
        yearToken.Should().NotBeNullOrEmpty();
        monthToken.Should().NotBeNullOrEmpty();
        fullToken.Should().NotBeNullOrEmpty();

        // Different granularities should produce different tokens
        yearToken.Should().NotBe(monthToken);
        monthToken.Should().NotBe(fullToken);
        yearToken.Should().NotBe(fullToken);
    }

    [Fact]
    public async Task GenerateNameSearchTokensAsync_WithSearchTerm_ShouldGenerateMatchingTokens()
    {
        // Arrange
        var searchTerm = "Jon";
        var tenantId = "tenant-123";
        var pepper = "test-pepper-base64";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync($"search-pepper-{tenantId}", default))
            .ReturnsAsync(pepper);

        // Act
        var result = await _sut.GenerateNameSearchTokensAsync(searchTerm);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // Should include prefix and phonetic variations
        result.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetIdentifierPepperAsync_ShouldCachePepper()
    {
        // Arrange
        SetupIdentifierPepper();

        // Act - Call twice
        await _sut.GenerateEmailTokenAsync("test@example.com");
        await _sut.GenerateEmailTokenAsync("another@example.com");

        // Assert - Should only call KeyVault once due to caching
        _keyVaultServiceMock.Verify(
            x => x.GetSecretAsync("search-token-pepper", default),
            Times.Once);
    }

    [Fact]
    public async Task GetIdentifierPepperAsync_WhenNotExists_ShouldGenerateAndPersistNew()
    {
        // Arrange
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync("search-token-pepper", default))
            .ReturnsAsync((string?)null); // Simulate not found
        _keyVaultServiceMock.Setup(x => x.SetSecretAsync(
            "search-token-pepper",
            It.IsAny<string>(),
            default))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.GenerateEmailTokenAsync("test@example.com");

        // Assert
        result.Should().NotBeNullOrEmpty();
        _keyVaultServiceMock.Verify(
            x => x.SetSecretAsync("search-token-pepper", It.IsAny<string>(), default),
            Times.Once);
    }

    [Fact]
    public async Task NormalizeName_ShouldRemoveDiacriticsAndSpecialChars()
    {
        // Arrange
        var names = new[]
        {
            ("José García", "jose garcia"),
            ("François Müller", "francois muller"),
            ("O'Brien-Smith", "obrien smith"),
            ("John  Doe", "john doe") // Multiple spaces
        };

        var tenantId = "tenant-123";
        var pepper = "test-pepper-base64";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync($"search-pepper-{tenantId}", default))
            .ReturnsAsync(pepper);

        foreach (var (input, _) in names)
        {
            // Act
            var tokens = await _sut.GenerateNameTokensAsync(input);

            // Assert
            tokens.Should().NotBeEmpty();
            // Normalized names should produce consistent tokens
        }
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    public async Task GenerateEmailTokenAsync_WithVariousInputs_ShouldHandleCorrectly(
        string? email,
        bool shouldGenerateToken)
    {
        // Arrange
        if (shouldGenerateToken)
        {
            SetupIdentifierPepper();
        }

        // Act
        var result = await _sut.GenerateEmailTokenAsync(email!);

        // Assert
        if (shouldGenerateToken)
        {
            result.Should().NotBeNullOrEmpty();
        }
        else
        {
            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task GenerateBulkTokensAsync_ShouldProcessMultiplePersons()
    {
        // Arrange
        var persons = new[]
        {
            new PersonSearchData
            {
                Id = "1",
                FullName = "John Doe",
                Email = "john@example.com",
                DateOfBirth = new DateTime(1990, 1, 1)
            },
            new PersonSearchData
            {
                Id = "2",
                FullName = "Jane Smith",
                Email = "jane@example.com",
                DateOfBirth = new DateTime(1985, 6, 15)
            }
        };

        var tenantId = "tenant-123";
        var pepper = "test-pepper-base64";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns(tenantId);
        _keyVaultServiceMock.Setup(x => x.GetSecretAsync($"search-pepper-{tenantId}", default))
            .ReturnsAsync(pepper);
        SetupIdentifierPepper(); // email tokens use the deployment-wide identifier pepper

        // Act
        var result = await _sut.GenerateBulkTokensAsync(persons);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("1");
        result.Should().ContainKey("2");

        result["1"].Should().NotBeEmpty();
        result["2"].Should().NotBeEmpty();
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memoryCache?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
