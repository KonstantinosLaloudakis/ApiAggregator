using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services;
using ApiAggregator.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApiAggregator.Tests.Services;

public class AggregationServiceTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<AggregationService>> _loggerMock;

    public AggregationServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AggregationService>>();
    }

    [Fact]
    public void AggregationRequest_ShouldAllowNullableProperties()
    {
        // Arrange & Act
        var request = new AggregationRequest
        {
            City = null,
            Query = "test",
            Category = null
        };

        // Assert
        Assert.Null(request.City);
        Assert.Equal("test", request.Query);
        Assert.Null(request.Category);
    }

    [Fact]
    public void AggregatedResponse_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var response = new AggregatedResponse();

        // Assert
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
        Assert.NotNull(response.Errors);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void AggregatedResponse_ShouldStoreWeatherDataInDictionary()
    {
        // Arrange
        var weather = new WeatherData
        {
            City = "London",
            Temperature = 20,
            Description = "Cloudy"
        };

        // Act
        var response = new AggregatedResponse();
        response.Data["weather"] = weather;

        // Assert
        Assert.True(response.Data.ContainsKey("weather"));
        var storedWeather = response.Data["weather"] as WeatherData;
        Assert.NotNull(storedWeather);
        Assert.Equal("London", storedWeather.City);
        Assert.Equal(20, storedWeather.Temperature);
    }

    [Fact]
    public void AggregatedResponse_ShouldStoreNewsArticlesInDictionary()
    {
        // Arrange
        var articles = new List<NewsArticle>
        {
            new() { Title = "Article 1", PublishedAt = DateTime.UtcNow },
            new() { Title = "Article 2", PublishedAt = DateTime.UtcNow.AddHours(-1) }
        };

        // Act
        var response = new AggregatedResponse();
        response.Data["news"] = articles;

        // Assert
        Assert.True(response.Data.ContainsKey("news"));
        var storedArticles = response.Data["news"] as List<NewsArticle>;
        Assert.NotNull(storedArticles);
        Assert.Equal(2, storedArticles.Count);
        Assert.Equal("Article 1", storedArticles[0].Title);
    }

    [Fact]
    public void AggregatedResponse_ShouldStoreGitHubRepositoriesInDictionary()
    {
        // Arrange
        var repos = new List<GitHubRepository>
        {
            new() { Name = "Repo1", Stars = 100 },
            new() { Name = "Repo2", Stars = 200 }
        };

        // Act
        var response = new AggregatedResponse();
        response.Data["github"] = repos;

        // Assert
        Assert.True(response.Data.ContainsKey("github"));
        var storedRepos = response.Data["github"] as List<GitHubRepository>;
        Assert.NotNull(storedRepos);
        Assert.Equal(2, storedRepos.Count);
        Assert.Equal("Repo1", storedRepos[0].Name);
    }

    [Fact]
    public void AggregatedResponse_ShouldSupportMultipleDataSources()
    {
        // Arrange
        var weather = new WeatherData { City = "Paris", Temperature = 25 };
        var articles = new List<NewsArticle> { new() { Title = "News" } };
        var repos = new List<GitHubRepository> { new() { Name = "Repo" } };

        // Act
        var response = new AggregatedResponse();
        response.Data["weather"] = weather;
        response.Data["news"] = articles;
        response.Data["github"] = repos;

        // Assert
        Assert.Equal(3, response.Data.Count);
        Assert.True(response.Data.ContainsKey("weather"));
        Assert.True(response.Data.ContainsKey("news"));
        Assert.True(response.Data.ContainsKey("github"));
    }
}
