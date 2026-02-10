using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Services.Interfaces;

/// <summary>
/// Service for aggregating data from multiple external APIs
/// </summary>
public interface IAggregationService
{
    /// <summary>
    /// Fetches and aggregates data from all configured external APIs
    /// </summary>
    /// <param name="request">The aggregation request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated response containing data from all APIs</returns>
    Task<AggregatedResponse> AggregateDataAsync(AggregationRequest request, CancellationToken cancellationToken = default);
}
