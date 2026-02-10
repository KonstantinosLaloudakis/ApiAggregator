using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

/// <summary>
/// Controller for aggregating data from multiple external APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AggregationController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly IEnumerable<IApiPlugin> _plugins;
    private readonly ILogger<AggregationController> _logger;

    // Valid categories are dynamically built from registered plugins + "all"
    private readonly HashSet<string> _validCategories;

    // Valid sort orders
    private static readonly HashSet<string> ValidSortOrders = new(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };

    public AggregationController(
        IAggregationService aggregationService,
        IEnumerable<IApiPlugin> plugins,
        ILogger<AggregationController> logger)
    {
        _aggregationService = aggregationService;
        _plugins = plugins;
        _logger = logger;

        _validCategories = _plugins
            .Select(p => p.Category.ToLowerInvariant())
            .Append("all")
            .ToHashSet();
    }

    /// <summary>
    /// Fetches and aggregates data from all configured external APIs
    /// </summary>
    /// <param name="city">City for weather data (e.g., "London")</param>
    /// <param name="query">Search query for news and GitHub repositories</param>
    /// <param name="category">Filter by category: weather, news, github, or all (default)</param>
    /// <param name="sortBy">Sort field: date, stars (availability depends on data source)</param>
    /// <param name="sortOrder">Sort order: asc or desc (default: desc)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated response containing data from requested APIs</returns>
    /// <response code="200">Returns the aggregated data</response>
    /// <response code="400">If validation fails (missing query, invalid category, etc.)</response>
    [HttpGet]
    [ProducesResponseType(typeof(AggregatedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AggregatedResponse>> GetAggregatedData(
        [FromQuery] string? city,
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate that at least one search parameter is provided
        if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new ErrorResponse { Message = "At least one of 'city' or 'query' parameters is required.", StatusCode = 400 });
        }

        // Validate pagination
        if (page < 1)
        {
            return BadRequest(new ErrorResponse { Message = "Page number must be greater than or equal to 1.", StatusCode = 400 });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ErrorResponse { Message = "Page size must be between 1 and 100.", StatusCode = 400 });
        }

        // Validate category against registered plugins
        if (!string.IsNullOrWhiteSpace(category) && !_validCategories.Contains(category.ToLowerInvariant()))
        {
            return BadRequest(new ErrorResponse 
            { 
                Message = $"Invalid category '{category}'. Valid categories are: {string.Join(", ", _validCategories.Order())}.",
                StatusCode = 400
            });
        }

        // Validate sort order
        if (!string.IsNullOrWhiteSpace(sortOrder) && !ValidSortOrders.Contains(sortOrder))
        {
            return BadRequest(new ErrorResponse { Message = $"Invalid sortOrder '{sortOrder}'. Valid values are: asc, desc.", StatusCode = 400 });
        }

        // Validate sortBy against sortable plugins
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var allSortFields = _plugins
                .OfType<ISortablePlugin>()
                .SelectMany(p => p.SupportedSortFields)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!allSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new ErrorResponse 
                { 
                    Message = $"Invalid sortBy '{sortBy}'. Available sort fields are: {string.Join(", ", allSortFields.Order())}.",
                    StatusCode = 400
                });
            }
        }

        // Validate city is provided when requesting weather category specifically
        if (category?.Equals("weather", StringComparison.OrdinalIgnoreCase) == true 
            && string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new ErrorResponse { Message = "The 'city' parameter is required when category is 'weather'.", StatusCode = 400 });
        }

        _logger.LogInformation(
            "Aggregation request - City: {City}, Query: {Query}, Category: {Category}, Page: {Page}, Size: {PageSize}",
            city, query, category, page, pageSize);

        var request = new AggregationRequest
        {
            City = city,
            Query = query,
            Category = category,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = pageSize
        };

        var result = await _aggregationService.AggregateDataAsync(request, cancellationToken);

        return Ok(result);
    }
}
