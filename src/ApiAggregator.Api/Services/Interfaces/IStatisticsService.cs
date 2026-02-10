using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Services.Interfaces;

/// <summary>
/// Service for tracking and reporting API request statistics
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Records a request to an external API
    /// </summary>
    /// <param name="apiName">Name of the API</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="success">Whether the request was successful</param>
    void RecordRequest(string apiName, double responseTimeMs, bool success);
    
    /// <summary>
    /// Gets statistics for all tracked APIs
    /// </summary>
    /// <returns>Statistics response with performance buckets</returns>
    StatisticsResponse GetStatistics();
    
    /// <summary>
    /// Gets statistics for a specific API
    /// </summary>
    /// <param name="apiName">Name of the API</param>
    /// <returns>Statistics for the specified API</returns>
    ApiStatistics? GetApiStatistics(string apiName);
}
