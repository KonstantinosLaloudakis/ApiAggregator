namespace ApiAggregator.Api.Configuration;

/// <summary>
/// Configuration settings for all external APIs
/// </summary>
public class ApiSettings
{
    public OpenWeatherMapSettings OpenWeatherMap { get; set; } = new();
    public NewsApiSettings NewsApi { get; set; } = new();
    public GitHubSettings GitHub { get; set; } = new();
}

public class OpenWeatherMapSettings
{
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5";
    public string ApiKey { get; set; } = string.Empty;
}

public class NewsApiSettings
{
    public string BaseUrl { get; set; } = "https://newsapi.org/v2";
    public string ApiKey { get; set; } = string.Empty;
}

public class GitHubSettings
{
    public string BaseUrl { get; set; } = "https://api.github.com";
}

/// <summary>
/// Cache configuration settings
/// </summary>
public class CacheSettings
{
    public int ExpirationMinutes { get; set; } = 5;
}
