using System.Collections.Concurrent;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;

namespace ApiAggregator.Api.Services;

/// <summary>
/// Thread-safe service for tracking API request statistics.
/// Uses a sliding window (bounded ConcurrentQueue) per API to prevent
/// unbounded memory growth while retaining recent data for statistics.
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<RequestRecord>> _requests = new();
    private readonly ILogger<StatisticsService> _logger;

    // Performance bucket thresholds (in milliseconds)
    private const double FastThreshold = 500;
    private const double AverageThreshold = 1000;

    /// <summary>
    /// Maximum number of request records kept per API.
    /// Oldest records are evicted once this limit is reached.
    /// </summary>
    public const int MaxRecordsPerApi = 1000;

    public StatisticsService(ILogger<StatisticsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a request to an external API (thread-safe).
    /// Evicts the oldest records when the sliding window limit is exceeded.
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

        var queue = _requests.GetOrAdd(apiName, _ => new ConcurrentQueue<RequestRecord>());
        queue.Enqueue(record);

        // Trim oldest records if over the limit
        while (queue.Count > MaxRecordsPerApi)
        {
            queue.TryDequeue(out _);
        }

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
