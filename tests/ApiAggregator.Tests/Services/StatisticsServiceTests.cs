using ApiAggregator.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApiAggregator.Tests.Services;

public class StatisticsServiceTests
{
    private readonly StatisticsService _sut;
    private readonly Mock<ILogger<StatisticsService>> _loggerMock;

    public StatisticsServiceTests()
    {
        _loggerMock = new Mock<ILogger<StatisticsService>>();
        _sut = new StatisticsService(_loggerMock.Object);
    }

    [Fact]
    public void RecordRequest_ShouldAddNewApiRecord()
    {
        // Act
        _sut.RecordRequest("TestAPI", 150, true);

        // Assert
        var stats = _sut.GetApiStatistics("TestAPI");
        Assert.NotNull(stats);
        Assert.Equal("TestAPI", stats.ApiName);
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(150, stats.AverageResponseTimeMs);
    }

    [Fact]
    public void RecordRequest_ShouldAccumulateMultipleRequests()
    {
        // Act
        _sut.RecordRequest("TestAPI", 100, true);
        _sut.RecordRequest("TestAPI", 200, true);
        _sut.RecordRequest("TestAPI", 300, true);

        // Assert
        var stats = _sut.GetApiStatistics("TestAPI");
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalRequests);
        Assert.Equal(200, stats.AverageResponseTimeMs); // Average of 100, 200, 300
    }

    [Fact]
    public void RecordRequest_ShouldCategorizeFastRequests()
    {
        // Act - Fast request (< 100ms)
        _sut.RecordRequest("TestAPI", 50, true);

        // Assert
        var stats = _sut.GetApiStatistics("TestAPI");
        Assert.NotNull(stats);
        Assert.Equal(1, stats.PerformanceBuckets.Fast);
        Assert.Equal(0, stats.PerformanceBuckets.Average);
        Assert.Equal(0, stats.PerformanceBuckets.Slow);
    }

    [Fact]
    public void RecordRequest_ShouldCategorizeAverageRequests()
    {
        // Act - Average request (500-1000ms)
        _sut.RecordRequest("TestAPI", 600, true);

        // Assert
        var stats = _sut.GetApiStatistics("TestAPI");
        Assert.NotNull(stats);
        Assert.Equal(0, stats.PerformanceBuckets.Fast);
        Assert.Equal(1, stats.PerformanceBuckets.Average);
        Assert.Equal(0, stats.PerformanceBuckets.Slow);
    }

    [Fact]
    public void RecordRequest_ShouldCategorizeSlowRequests()
    {
        // Act - Slow request (> 1000ms)
        _sut.RecordRequest("TestAPI", 1200, true);

        // Assert
        var stats = _sut.GetApiStatistics("TestAPI");
        Assert.NotNull(stats);
        Assert.Equal(0, stats.PerformanceBuckets.Fast);
        Assert.Equal(0, stats.PerformanceBuckets.Average);
        Assert.Equal(1, stats.PerformanceBuckets.Slow);
    }

    [Fact]
    public void RecordRequest_ShouldHandleMultipleApis()
    {
        // Act
        _sut.RecordRequest("API1", 100, true);
        _sut.RecordRequest("API2", 200, true);
        _sut.RecordRequest("API3", 300, true);

        // Assert
        var allStats = _sut.GetStatistics();
        Assert.Equal(3, allStats.Apis.Count);
    }

    [Fact]
    public void GetStatistics_ShouldReturnEmptyWhenNoRequests()
    {
        // Act
        var stats = _sut.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Empty(stats.Apis);
    }

    [Fact]
    public void GetApiStatistics_ShouldReturnNullForUnknownApi()
    {
        // Act
        var stats = _sut.GetApiStatistics("UnknownAPI");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public async Task RecordRequest_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int requestCount = 100;

        // Act - Simulate concurrent requests
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(Task.Run(() => _sut.RecordRequest("ConcurrentAPI", 100, true)));
        }
        await Task.WhenAll(tasks);

        // Assert
        var stats = _sut.GetApiStatistics("ConcurrentAPI");
        Assert.NotNull(stats);
        Assert.Equal(requestCount, stats.TotalRequests);
    }

    [Fact]
    public void RecordRequest_ShouldEvictOldestRecordsWhenLimitExceeded()
    {
        // Arrange - exceed the sliding window limit
        var totalRecords = StatisticsService.MaxRecordsPerApi + 100;

        // Act
        for (int i = 0; i < totalRecords; i++)
        {
            _sut.RecordRequest("BoundedAPI", i, true);
        }

        // Assert - only the most recent MaxRecordsPerApi records should remain
        var stats = _sut.GetApiStatistics("BoundedAPI");
        Assert.NotNull(stats);
        Assert.Equal(StatisticsService.MaxRecordsPerApi, stats.TotalRequests);

        // The average should reflect only the last 1000 records (values 100..1099)
        var expectedAvg = Math.Round(Enumerable.Range(100, StatisticsService.MaxRecordsPerApi).Average(), 2);
        Assert.Equal(expectedAvg, stats.AverageResponseTimeMs);
    }
}
