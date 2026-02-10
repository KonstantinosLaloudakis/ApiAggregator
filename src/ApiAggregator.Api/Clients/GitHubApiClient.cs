using System.Diagnostics;
using System.Net.Http.Json;
using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ApiAggregator.Api.Clients;

/// <summary>
/// Client for fetching repository data from GitHub API
/// </summary>
public class GitHubApiClient : IApiPlugin, ISortablePlugin
{
    private readonly HttpClient _httpClient;
    private readonly IStatisticsService _statisticsService;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubApiClient> _logger;

    public string Name => "GitHub";
    public string Category => "github";

    public IReadOnlyList<string> SupportedSortFields => new[] { "date", "stars" };

    async Task<object?> IApiPlugin.FetchDataAsync(string query, int page, int pageSize, CancellationToken ct) 
        => await FetchRepositoriesAsync(query, page, pageSize, ct);

    public object? SortData(object? data, string sortBy, string sortOrder)
    {
        if (data is not List<GitHubRepository> repos) return data;

        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "date" => isDescending
                ? repos.OrderByDescending(r => r.UpdatedAt).ToList()
                : repos.OrderBy(r => r.UpdatedAt).ToList(),
            "stars" => isDescending
                ? repos.OrderByDescending(r => r.Stars).ToList()
                : repos.OrderBy(r => r.Stars).ToList(),
            _ => repos
        };
    }

    public GitHubApiClient(
        HttpClient httpClient,
        IStatisticsService statisticsService,
        IOptions<ApiSettings> settings,
        ILogger<GitHubApiClient> logger)
    {
        _httpClient = httpClient;
        _statisticsService = statisticsService;
        _settings = settings.Value.GitHub;
        _logger = logger;
    }

    public async Task<List<GitHubRepository>?> FetchRepositoriesAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Query parameter is empty, skipping GitHub fetch");
            return new List<GitHubRepository>();
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var url = $"{_settings.BaseUrl}/search/repositories?q={Uri.EscapeDataString(query)}&sort=stars&order=desc&page={page}&per_page={pageSize}";
            
            _logger.LogInformation("Fetching GitHub repositories for query: {Query}", query);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<GitHubSearchResponse>(cancellationToken);
            
            if (apiResponse?.Items == null)
            {
                _logger.LogWarning("Received null or empty response from GitHub API");
                return new List<GitHubRepository>();
            }

            success = true;
            
            return apiResponse.Items
                .Where(r => r != null)
                .Select(r => new GitHubRepository
                {
                    Name = r.Name ?? string.Empty,
                    FullName = r.Full_name ?? string.Empty,
                    Description = r.Description ?? string.Empty,
                    Url = r.Html_url ?? string.Empty,
                    Language = r.Language ?? string.Empty,
                    Stars = r.Stargazers_count,
                    Forks = r.Forks_count,
                    OpenIssues = r.Open_issues_count,
                    CreatedAt = r.Created_at ?? DateTime.UtcNow,
                    UpdatedAt = r.Updated_at ?? DateTime.UtcNow
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching GitHub repos for query: {Query}", query);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout fetching GitHub repos for query: {Query}", query);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching GitHub repos for query: {Query}", query);
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _statisticsService.RecordRequest(Name, stopwatch.ElapsedMilliseconds, success);
        }
    }
}
