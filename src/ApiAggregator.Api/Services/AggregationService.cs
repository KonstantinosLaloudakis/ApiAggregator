using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;

namespace ApiAggregator.Api.Services;

/// <summary>
/// Service for aggregating data from multiple external APIs
/// Uses plugin architecture for easy extensibility - new APIs are auto-discovered via DI
/// </summary>
public class AggregationService : IAggregationService
{
    private readonly IEnumerable<IApiPlugin> _plugins;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IEnumerable<IApiPlugin> plugins,
        ICacheService cacheService,
        ILogger<AggregationService> logger)
    {
        _plugins = plugins;
        _cacheService = cacheService;
        _logger = logger;
        
        _logger.LogInformation("AggregationService initialized with {Count} plugins: {Plugins}",
            _plugins.Count(), string.Join(", ", _plugins.Select(p => p.Name)));
    }

    /// <summary>
    /// Fetches and aggregates data from all configured external APIs in parallel
    /// Plugins are auto-discovered via DI - just register new IApiPlugin implementations
    /// </summary>
    public async Task<AggregatedResponse> AggregateDataAsync(AggregationRequest request, CancellationToken cancellationToken = default)
    {
        var response = new AggregatedResponse();
        var errors = new List<string>();

        _logger.LogInformation(
            "Starting aggregation - City: {City}, Query: {Query}, Category: {Category}",
            request.City, request.Query, request.Category);

        // Determine query to use (city for weather, query for others)
        var query = request.Query ?? request.City ?? string.Empty;
        
        // Filter plugins by category
        var category = request.Category?.ToLowerInvariant() ?? "all";
        var filteredPlugins = _plugins
            .Where(p => category == "all" || p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogDebug("Filtered to {Count} plugins for category '{Category}'", 
            filteredPlugins.Count, category);

        // Create tasks for parallel execution with caching
        var pluginTasks = filteredPlugins.Select(async plugin =>
        {
            // Use appropriate query based on plugin category
            var pluginQuery = plugin.Category == "weather" ? request.City : request.Query;
            
            if (string.IsNullOrWhiteSpace(pluginQuery))
            {
                _logger.LogDebug("Skipping {Plugin} - no query provided for category {Category}", 
                    plugin.Name, plugin.Category);
                return (plugin, Result: (object?)null, Error: (string?)null);
            }

            try
            {
                var cacheKey = $"{plugin.Category}:{pluginQuery}";
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    () => plugin.FetchDataAsync(pluginQuery, cancellationToken),
                    cancellationToken);
                
                return (plugin, Result: result, Error: (string?)null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data from {Plugin}", plugin.Name);
                return (plugin, Result: (object?)null, Error: $"Failed to fetch from {plugin.Name}");
            }
        }).ToList();

        // Wait for all tasks to complete in parallel
        var results = await Task.WhenAll(pluginTasks);

        // Process results - all data goes into the dynamic Data dictionary
        foreach (var (plugin, result, error) in results)
        {
            if (error != null)
            {
                errors.Add(error);
                continue;
            }

            if (result == null) continue;

            // Add to dynamic Data dictionary (no hardcoded type handling!)
            response.Data[plugin.Category] = result;

            // Apply sorting if plugin supports it and sort parameters are provided
            if (!string.IsNullOrWhiteSpace(request.SortBy) 
                && plugin is ISortablePlugin sortable
                && sortable.SupportedSortFields.Contains(request.SortBy, StringComparer.OrdinalIgnoreCase))
            {
                var sortOrder = request.SortOrder ?? "desc";
                response.Data[plugin.Category] = sortable.SortData(result, request.SortBy, sortOrder);
                _logger.LogDebug("Applied sorting to {Plugin}: {SortBy} {SortOrder}", 
                    plugin.Name, request.SortBy, sortOrder);
            }
        }

        response.Errors = errors;
        response.Timestamp = DateTime.UtcNow;

        _logger.LogInformation(
            "Aggregation complete - Data sources: {Sources}, Errors: {ErrorCount}",
            string.Join(", ", response.Data.Keys), errors.Count);

        return response;
    }
}
