using System.Diagnostics;
using System.Net.Http.Json;
using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ApiAggregator.Api.Clients;

/// <summary>
/// Client for fetching news articles from News API
/// </summary>
public class NewsApiClient : IApiPlugin, ISortablePlugin
{
    private readonly HttpClient _httpClient;
    private readonly IStatisticsService _statisticsService;
    private readonly NewsApiSettings _settings;
    private readonly ILogger<NewsApiClient> _logger;

    public string Name => "NewsAPI";
    public string Category => "news";

    public IReadOnlyList<string> SupportedSortFields => new[] { "date" };

    async Task<object?> IApiPlugin.FetchDataAsync(string query, int page, int pageSize, CancellationToken ct) 
        => await FetchNewsDataAsync(query, page, pageSize, ct);

    public object? SortData(object? data, string sortBy, string sortOrder)
    {
        if (data is not List<NewsArticle> articles) return data;

        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "date" => isDescending
                ? articles.OrderByDescending(a => a.PublishedAt).ToList()
                : articles.OrderBy(a => a.PublishedAt).ToList(),
            _ => articles
        };
    }

    public NewsApiClient(
        HttpClient httpClient,
        IStatisticsService statisticsService,
        IOptions<ApiSettings> settings,
        ILogger<NewsApiClient> logger)
    {
        _httpClient = httpClient;
        _statisticsService = statisticsService;
        _settings = settings.Value.NewsApi;
        _logger = logger;
    }

    public async Task<List<NewsArticle>?> FetchNewsDataAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Query parameter is empty, skipping news fetch");
            return new List<NewsArticle>();
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var url = $"{_settings.BaseUrl}/everything?q={Uri.EscapeDataString(query)}&apiKey={_settings.ApiKey}&page={page}&pageSize={pageSize}&sortBy=publishedAt";
            
            _logger.LogInformation("Fetching news articles for query: {Query}", query);
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "ApiAggregator/1.0");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<NewsApiResponse>(cancellationToken);
            
            if (apiResponse?.Articles == null)
            {
                _logger.LogWarning("Received null or empty response from News API");
                return new List<NewsArticle>();
            }

            success = true;
            
            return apiResponse.Articles
                .Where(a => a != null)
                .Select(a => new NewsArticle
                {
                    Title = a.Title ?? string.Empty,
                    Description = a.Description ?? string.Empty,
                    Author = a.Author ?? string.Empty,
                    Source = a.Source?.Name ?? string.Empty,
                    Url = a.Url ?? string.Empty,
                    ImageUrl = a.UrlToImage ?? string.Empty,
                    PublishedAt = a.PublishedAt ?? DateTime.UtcNow
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching news for query: {Query}", query);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout fetching news for query: {Query}", query);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching news for query: {Query}", query);
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _statisticsService.RecordRequest(Name, stopwatch.ElapsedMilliseconds, success);
        }
    }
}
