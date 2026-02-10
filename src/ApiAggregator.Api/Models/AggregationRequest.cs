namespace ApiAggregator.Api.Models;

/// <summary>
/// Query parameters for the aggregation endpoint
/// </summary>
public class AggregationRequest
{
    /// <summary>Search query for news and GitHub</summary>
    public string? Query { get; set; }
    
    /// <summary>City for weather data</summary>
    public string? City { get; set; }
    
    /// <summary>Filter by category: weather, news, github, or all (default)</summary>
    public string? Category { get; set; }
    
    /// <summary>Sort by field: date, stars, relevance (availability depends on data source)</summary>
    public string? SortBy { get; set; }
    
    /// <summary>Sort order: asc or desc (default: desc)</summary>
    public string? SortOrder { get; set; }

    /// <summary>Page number (default: 1)</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of items per page (default: 10)</summary>
    public int PageSize { get; set; } = 10;
}
