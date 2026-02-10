using System.Collections.Concurrent;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;

namespace ApiAggregator.Api.Services;

/// <summary>
/// Thread-safe service for tracking API request statistics
/// Uses ConcurrentDictionary for thread-safe operations
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<RequestRecord>> _requests = new();
    private readonly ILogger<StatisticsService> _logger;

    // Performance bucket thresholds (in milliseconds)
    private const double FastThreshold = 100;
    private const double AverageThreshold = 200;

    public StatisticsService(ILogger<StatisticsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a request to an external API (thread-safe)
    /// </summary>
    public void RecordRequest(string apiName, double responseTimeMs, bool success)
    {
        var record = new RequestRecord
        {
            ApiName = apiName,
            ResponseTimeMs = responseTimeMs,
            Timestamp = DateTime.UtcNow,
            Success = success
        };

        _requests.AddOrUpdate(
            apiName,
            _ => new ConcurrentBag<RequestRecord> { record },
            (_, bag) =>
            {
                bag.Add(record);
                return bag;
            });

        _logger.LogDebug(
            "Recorded request for {ApiName}: {ResponseTimeMs}ms, Success: {Success}",
            apiName, responseTimeMs, success);
    }

    /// <summary>
    /// Gets statistics for all tracked APIs
    /// </summary>
    public StatisticsResponse GetStatistics()
    {
        var stats = _requests.Keys
            .Select(apiName => GetApiStatistics(apiName))
            .Where(s => s != null)
            .Cast<ApiStatistics>()
            .ToList();

        return new StatisticsResponse
        {
            Apis = stats,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets statistics for a specific API
    /// </summary>
    public ApiStatistics? GetApiStatistics(string apiName)
    {
        if (!_requests.TryGetValue(apiName, out var records) || records.IsEmpty)
        {
            return null;
        }

        var recordList = records.ToList();
        var totalRequests = recordList.Count;
        var avgResponseTime = recordList.Average(r => r.ResponseTimeMs);

        var buckets = new PerformanceBuckets
        {
            Fast = recordList.Count(r => r.ResponseTimeMs < FastThreshold),
            Average = recordList.Count(r => r.ResponseTimeMs >= FastThreshold && r.ResponseTimeMs < AverageThreshold),
            Slow = recordList.Count(r => r.ResponseTimeMs >= AverageThreshold)
        };

        return new ApiStatistics
        {
            ApiName = apiName,
            TotalRequests = totalRequests,
            AverageResponseTimeMs = Math.Round(avgResponseTime, 2),
            PerformanceBuckets = buckets
        };
    }
}
