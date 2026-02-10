namespace ApiAggregator.Api.Models;

/// <summary>
/// Statistics for API requests with performance bucketing
/// </summary>
public class ApiStatistics
{
    public string ApiName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public PerformanceBuckets PerformanceBuckets { get; set; } = new();
}

/// <summary>
/// Performance buckets for categorizing response times
/// </summary>
public class PerformanceBuckets
{
    /// <summary>Requests under 100ms</summary>
    public int Fast { get; set; }
    
    /// <summary>Requests between 100-200ms</summary>
    public int Average { get; set; }
    
    /// <summary>Requests over 200ms</summary>
    public int Slow { get; set; }
}

/// <summary>
/// Full statistics response containing all API stats
/// </summary>
public class StatisticsResponse
{
    public List<ApiStatistics> Apis { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual request record for tracking
/// </summary>
public class RequestRecord
{
    public string ApiName { get; set; } = string.Empty;
    public double ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}
