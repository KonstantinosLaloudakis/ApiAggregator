using System.Diagnostics;
using System.Net.Http.Json;
using ApiAggregator.Api.Configuration;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ApiAggregator.Api.Clients;

/// <summary>
/// Client for fetching weather data from OpenWeatherMap API
/// </summary>
public class WeatherApiClient : IApiPlugin
{
    private readonly HttpClient _httpClient;
    private readonly IStatisticsService _statisticsService;
    private readonly OpenWeatherMapSettings _settings;
    private readonly ILogger<WeatherApiClient> _logger;

    public string Name => "OpenWeatherMap";
    public string Category => "weather";

    async Task<object?> IApiPlugin.FetchDataAsync(string query, CancellationToken ct) 
        => await FetchWeatherDataAsync(query, ct);

    public WeatherApiClient(
        HttpClient httpClient,
        IStatisticsService statisticsService,
        IOptions<ApiSettings> settings,
        ILogger<WeatherApiClient> logger)
    {
        _httpClient = httpClient;
        _statisticsService = statisticsService;
        _settings = settings.Value.OpenWeatherMap;
        _logger = logger;
    }

    public async Task<WeatherData?> FetchWeatherDataAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            _logger.LogWarning("City parameter is empty, skipping weather fetch");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var url = $"{_settings.BaseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_settings.ApiKey}&units=metric";
            
            _logger.LogInformation("Fetching weather data for city: {City}", city);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadFromJsonAsync<OpenWeatherMapResponse>(cancellationToken);
            
            if (apiResponse == null)
            {
                _logger.LogWarning("Received null response from OpenWeatherMap API");
                return null;
            }

            success = true;
            
            return new WeatherData
            {
                City = apiResponse.Name ?? city,
                Country = apiResponse.Sys?.Country ?? string.Empty,
                Temperature = apiResponse.Main?.Temp ?? 0,
                FeelsLike = apiResponse.Main?.Feels_like ?? 0,
                Humidity = apiResponse.Main?.Humidity ?? 0,
                Description = apiResponse.Weather?.FirstOrDefault()?.Description ?? string.Empty,
                Icon = apiResponse.Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                WindSpeed = apiResponse.Wind?.Speed ?? 0,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching weather data for city: {City}", city);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout fetching weather data for city: {City}", city);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching weather data for city: {City}", city);
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _statisticsService.RecordRequest(Name, stopwatch.ElapsedMilliseconds, success);
        }
    }
}
