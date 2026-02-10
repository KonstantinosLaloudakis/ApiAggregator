using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ApiAggregator.Tests.Services;

public class CacheServiceTests
{
    private readonly CacheService _sut;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<CacheService>> _loggerMock;

    public CacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CacheService>>();
        
        var cacheSettings = Options.Create(new CacheSettings { ExpirationMinutes = 5 });
        _sut = new CacheService(_memoryCache, cacheSettings, _loggerMock.Object);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCallFactoryOnCacheMiss()
    {
        // Arrange
        var factoryCalled = false;
        
        // Act
        var result = await _sut.GetOrCreateAsync("testKey", () =>
        {
            factoryCalled = true;
            return Task.FromResult<string?>("testValue");
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.Equal("testValue", result);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnCachedValueOnCacheHit()
    {
        // Arrange
        var factoryCallCount = 0;
        Func<Task<string?>> factory = () =>
        {
            factoryCallCount++;
            return Task.FromResult<string?>("testValue");
        };

        // Act - First call populates cache
        await _sut.GetOrCreateAsync("testKey", factory);
        
        // Act - Second call should return cached value
        var result = await _sut.GetOrCreateAsync("testKey", factory);

        // Assert
        Assert.Equal(1, factoryCallCount);
        Assert.Equal("testValue", result);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldNotCacheNullValues()
    {
        // Arrange
        var factoryCallCount = 0;
        Func<Task<string?>> factory = () =>
        {
            factoryCallCount++;
            return Task.FromResult<string?>(null);
        };

        // Act - First call returns null
        await _sut.GetOrCreateAsync("testKey", factory);
        
        // Act - Second call should call factory again
        await _sut.GetOrCreateAsync("testKey", factory);

        // Assert
        Assert.Equal(2, factoryCallCount);
    }

    [Fact]
    public void Remove_ShouldRemoveCachedValue()
    {
        // Arrange
        _memoryCache.Set("testKey", "testValue");

        // Act
        _sut.Remove("testKey");

        // Assert
        Assert.False(_memoryCache.TryGetValue("testKey", out _));
    }
}
