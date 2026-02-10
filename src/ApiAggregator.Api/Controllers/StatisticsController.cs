using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

/// <summary>
/// Controller for retrieving API request statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsService statisticsService,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets statistics for all tracked external APIs
    /// </summary>
    /// <returns>Statistics including request counts and performance buckets per API</returns>
    /// <response code="200">Returns the statistics for all APIs</response>
    [HttpGet]
    [ProducesResponseType(typeof(StatisticsResponse), StatusCodes.Status200OK)]
    public ActionResult<StatisticsResponse> GetStatistics()
    {
        _logger.LogInformation("Statistics request received");
        
        var stats = _statisticsService.GetStatistics();
        
        return Ok(stats);
    }

    /// <summary>
    /// Gets statistics for a specific external API
    /// </summary>
    /// <param name="apiName">Name of the API (e.g., "OpenWeatherMap", "NewsAPI", "GitHub")</param>
    /// <returns>Statistics for the specified API</returns>
    /// <response code="200">Returns the statistics for the specified API</response>
    /// <response code="404">If the API has no recorded statistics</response>
    [HttpGet("{apiName}")]
    [ProducesResponseType(typeof(ApiStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ApiStatistics> GetApiStatistics(string apiName)
    {
        _logger.LogInformation("Statistics request received for API: {ApiName}", apiName);
        
        var stats = _statisticsService.GetApiStatistics(apiName);
        
        if (stats == null)
        {
            return NotFound(new { error = $"No statistics found for API: {apiName}" });
        }
        
        return Ok(stats);
    }
}
