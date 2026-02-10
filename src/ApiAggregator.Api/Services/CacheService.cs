using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ApiAggregator.Api.Services;

/// <summary>
/// Service for caching API responses using IMemoryCache
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _settings;
    private readonly ILogger<CacheService> _logger;

    public CacheService(
        IMemoryCache cache,
        IOptions<CacheSettings> settings,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets a cached value or creates it using the factory function
    /// </summary>
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}, fetching data", key);
        
        var value = await factory();
        
        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.ExpirationMinutes)
            };
            
            _cache.Set(key, value, cacheOptions);
            _logger.LogDebug("Cached value for key: {Key}, expires in {Minutes} minutes", key, _settings.ExpirationMinutes);
        }

        return value;
    }

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Removed cache entry for key: {Key}", key);
    }
}
