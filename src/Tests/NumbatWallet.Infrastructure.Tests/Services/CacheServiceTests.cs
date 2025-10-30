using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Services;
using System.Text;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class CacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _cacheService;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheServiceTests()
    {
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CacheService>>();
        _cacheService = new CacheService(_mockDistributedCache.Object, _mockLogger.Object);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new TestCacheItem { Id = 123, Name = "Test" };
        var json = JsonSerializer.Serialize(expectedValue, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        // Act
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedValue.Id);
        result.Name.Should().Be(expectedValue.Name);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existing-key";

        _mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithValue_StoresInCache()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheItem { Id = 456, Name = "Test Item" };
        byte[]? capturedBytes = null;
        DistributedCacheEntryOptions? capturedOptions = null;

        _mockDistributedCache
            .Setup(x => x.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((k, b, o, c) =>
            {
                capturedBytes = b;
                capturedOptions = o;
            })
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

        // Assert
        _mockDistributedCache.Verify(x => x.SetAsync(
            key,
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        capturedBytes.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedBytes!);
        var deserializedValue = JsonSerializer.Deserialize<TestCacheItem>(json, _jsonOptions);
        deserializedValue.Should().BeEquivalentTo(value);

        capturedOptions.Should().NotBeNull();
        capturedOptions!.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task SetAsync_WithNullValue_RemovesFromCache()
    {
        // Arrange
        var key = "test-key";

        _mockDistributedCache
            .Setup(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync<TestCacheItem>(key, null!, TimeSpan.FromMinutes(10));

        // Assert
        _mockDistributedCache.Verify(x => x.RemoveAsync(
            key,
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDistributedCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithKey_RemovesFromCache()
    {
        // Arrange
        var key = "test-key";

        _mockDistributedCache
            .Setup(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockDistributedCache.Verify(x => x.RemoveAsync(
            key,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheMiss_CallsFactoryAndStores()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new TestCacheItem { Id = 789, Name = "Factory Value" };

        _mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _mockDistributedCache
            .Setup(x => x.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async () => await Task.FromResult(expectedValue),
            TimeSpan.FromMinutes(5));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedValue.Id);
        result.Name.Should().Be(expectedValue.Name);

        _mockDistributedCache.Verify(x => x.GetAsync(
            key,
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDistributedCache.Verify(x => x.SetAsync(
            key,
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheHit_DoesNotCallFactory()
    {
        // Arrange
        var key = "test-key";
        var cachedValue = new TestCacheItem { Id = 999, Name = "Cached Value" };
        var json = JsonSerializer.Serialize(cachedValue, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        _mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bytes);

        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async () =>
            {
                factoryCalled = true;
                return await Task.FromResult(new TestCacheItem { Id = 111, Name = "Factory Value" });
            },
            TimeSpan.FromMinutes(5));

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(cachedValue.Id);
        result.Name.Should().Be(cachedValue.Name);
        factoryCalled.Should().BeFalse();

        _mockDistributedCache.Verify(x => x.GetAsync(
            key,
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDistributedCache.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_WithPattern_LogsWarning()
    {
        // Arrange
        var prefix = "user:";

        // Act
        await _cacheService.RemoveByPrefixAsync(prefix);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("RemoveByPrefix is not implemented")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    // Test helper class
    private class TestCacheItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}