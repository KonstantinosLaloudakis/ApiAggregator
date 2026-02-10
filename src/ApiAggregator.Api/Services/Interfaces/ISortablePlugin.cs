namespace ApiAggregator.Api.Services.Interfaces;

/// <summary>
/// Optional interface for API plugins that support sorting their results.
/// Plugins returning collections (e.g., news articles, repositories) should implement this.
/// Plugins returning single objects (e.g., weather) can skip this.
/// </summary>
public interface ISortablePlugin
{
    /// <summary>
    /// Returns the list of supported sort fields for this plugin (e.g., "date", "stars")
    /// </summary>
    IReadOnlyList<string> SupportedSortFields { get; }

    /// <summary>
    /// Sorts the fetched data by the specified field and order
    /// </summary>
    /// <param name="data">The data returned by FetchDataAsync</param>
    /// <param name="sortBy">Field to sort by (e.g., "date", "stars")</param>
    /// <param name="sortOrder">Sort direction: "asc" or "desc"</param>
    /// <returns>The sorted data</returns>
    object? SortData(object? data, string sortBy, string sortOrder);
}
