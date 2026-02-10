namespace ApiAggregator.Api.Services.Interfaces;

/// <summary>
/// Service for caching API responses
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value or fetches it using the provided factory
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create the value if not cached</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached or newly fetched value</returns>
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    void Remove(string key);
}
