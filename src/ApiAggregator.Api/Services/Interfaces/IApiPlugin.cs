namespace ApiAggregator.Api.Services.Interfaces;

/// <summary>
/// Plugin interface for external API integrations.
/// Implement this interface to add a new API source to the aggregator.
/// </summary>
public interface IApiPlugin
{
    /// <summary>
    /// Unique name for this API (e.g., "OpenWeatherMap", "NewsAPI", "GitHub")
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Category for filtering (e.g., "weather", "news", "github")
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// Fetches data from the external API
    /// </summary>
    /// <param name="query">Search query or city name depending on API</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API-specific data object, or null if fetch failed</returns>
    Task<object?> FetchDataAsync(string query, CancellationToken cancellationToken = default);
}
