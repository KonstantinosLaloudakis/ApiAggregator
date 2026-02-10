namespace ApiAggregator.Api.Models;

/// <summary>
/// Unified response containing aggregated data from all external APIs
/// Data is organized by plugin category in the Data dictionary
/// </summary>
public class AggregatedResponse
{
    /// <summary>
    /// Dynamic data from all plugins, keyed by category (e.g., "weather", "news", "github")
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();
    
    /// <summary>
    /// Timestamp when the aggregation was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Any errors that occurred during aggregation
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
