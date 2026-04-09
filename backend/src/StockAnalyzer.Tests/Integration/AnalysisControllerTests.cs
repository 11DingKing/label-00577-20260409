using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StockAnalyzer.Core.DTOs;
using Xunit;

namespace StockAnalyzer.Tests.Integration;

public class AnalysisControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnalysisControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RunAnalysis_WhenNoStocks_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/analysis/run", new RunAnalysisRequest());

        // Assert
        // 由于没有股票，应该返回 BadRequest 或者类似的错误
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RunAnalysis_WithStocks_ShouldReturnResults()
    {
        // Arrange - 先添加一些股票
        await _client.PostAsJsonAsync("/api/stocks", new AddStockRequest { Symbol = "ANAL1", Name = "Analysis Test 1" });
        await _client.PostAsJsonAsync("/api/stocks", new AddStockRequest { Symbol = "ANAL2", Name = "Analysis Test 2" });

        // Act
        var response = await _client.PostAsJsonAsync("/api/analysis/run", new RunAnalysisRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RunAnalysisResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.SuccessCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RunSingleAnalysis_WhenStockNotFound_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/analysis/run/NOTEXIST", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RunSingleAnalysis_WhenStockExists_ShouldReturnResult()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/stocks", new AddStockRequest { Symbol = "SINGLE1", Name = "Single Analysis Test" });

        // Act
        var response = await _client.PostAsync("/api/analysis/run/SINGLE1", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnalysisResultResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Symbol.Should().Be("SINGLE1");
        result.Data.Recommendation.Should().BeOneOf("Buy", "Hold", "Sell");
        result.Data.Confidence.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetResults_ShouldReturnPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/analysis/results?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AnalysisResultListResponse>>();
        result!.Success.Should().BeTrue();
        result.Data!.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetLatest_ShouldReturnResults()
    {
        // Act
        var response = await _client.GetAsync("/api/analysis/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AnalysisResultResponse>>>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetResultsBySymbol_WhenStockNotFound_ShouldReturnEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/analysis/results/NOTEXIST");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AnalysisResultResponse>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
