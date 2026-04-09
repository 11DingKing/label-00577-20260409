using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StockAnalyzer.Core.DTOs;
using Xunit;

namespace StockAnalyzer.Tests.Integration;

public class StatisticsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StatisticsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConsecutive_ShouldReturnResults()
    {
        // Act
        var response = await _client.GetAsync("/api/statistics/consecutive?days=3&recommendation=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ConsecutiveQueryResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Days.Should().Be(3);
        result.Data.Recommendation.Should().Be("Buy");
    }

    [Fact]
    public async Task GetConsecutive_WithInvalidDays_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/statistics/consecutive?days=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSummary_ShouldReturnStats()
    {
        // Act
        var response = await _client.GetAsync("/api/statistics/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StatisticsSummaryResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTrend_WhenStockNotFound_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/statistics/trend/NOTEXIST");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTrend_WhenStockExists_ShouldReturnTrend()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/stocks", new AddStockRequest { Symbol = "TREND1", Name = "Trend Test" });

        // Act
        var response = await _client.GetAsync("/api/statistics/trend/TREND1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StockTrendResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Symbol.Should().Be("TREND1");
    }

    [Fact]
    public async Task GetTrend_WithInvalidDays_ShouldReturnBadRequest()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/stocks", new AddStockRequest { Symbol = "TREND2", Name = "Trend Test 2" });

        // Act
        var response = await _client.GetAsync("/api/statistics/trend/TREND2?days=500");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
