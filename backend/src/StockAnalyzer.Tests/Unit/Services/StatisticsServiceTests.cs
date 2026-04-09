using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Core.Services;
using Xunit;

namespace StockAnalyzer.Tests.Unit.Services;

public class StatisticsServiceTests
{
    private readonly Mock<IStockRepository> _stockRepositoryMock;
    private readonly Mock<IAnalysisRepository> _analysisRepositoryMock;
    private readonly Mock<ILogger<StatisticsService>> _loggerMock;
    private readonly StatisticsService _sut;

    public StatisticsServiceTests()
    {
        _stockRepositoryMock = new Mock<IStockRepository>();
        _analysisRepositoryMock = new Mock<IAnalysisRepository>();
        _loggerMock = new Mock<ILogger<StatisticsService>>();
        
        _sut = new StatisticsService(
            _stockRepositoryMock.Object,
            _analysisRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetConsecutiveStocksAsync_WhenNoStocks_ShouldReturnEmpty()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Stock>());
        var request = new ConsecutiveQueryRequest { Days = 3, Recommendation = Recommendation.Buy };

        // Act
        var result = await _sut.GetConsecutiveStocksAsync(request);

        // Assert
        result.Stocks.Should().BeEmpty();
        result.TotalFound.Should().Be(0);
    }

    [Fact]
    public async Task GetConsecutiveStocksAsync_WhenConsecutiveBuyDays_ShouldReturnStock()
    {
        // Arrange
        var stock = new Stock { Id = 1, Symbol = "AAPL", Name = "Apple Inc." };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = new List<AnalysisResult>
        {
            new() { Id = 3, StockId = 1, Stock = stock, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Test" },
            new() { Id = 2, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Buy, Confidence = 80, Reasoning = "Test" },
            new() { Id = 1, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-2), Recommendation = Recommendation.Buy, Confidence = 75, Reasoning = "Test" }
        };

        _stockRepositoryMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Stock> { stock });
        _analysisRepositoryMock.Setup(x => x.GetConsecutiveResultsAsync(1, Recommendation.Buy, It.IsAny<DateOnly>(), 3))
            .ReturnsAsync(results);

        var request = new ConsecutiveQueryRequest { Days = 3, Recommendation = Recommendation.Buy };

        // Act
        var result = await _sut.GetConsecutiveStocksAsync(request);

        // Assert
        result.Stocks.Should().HaveCount(1);
        result.Stocks.First().Symbol.Should().Be("AAPL");
        result.Stocks.First().ConsecutiveDays.Should().Be(3);
    }

    [Fact]
    public async Task GetConsecutiveStocksAsync_WhenNotConsecutive_ShouldNotReturnStock()
    {
        // Arrange
        var stock = new Stock { Id = 1, Symbol = "AAPL", Name = "Apple Inc." };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = new List<AnalysisResult>
        {
            new() { Id = 3, StockId = 1, Stock = stock, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Test" },
            new() { Id = 2, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Hold, Confidence = 80, Reasoning = "Test" }, // 中断
            new() { Id = 1, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-2), Recommendation = Recommendation.Buy, Confidence = 75, Reasoning = "Test" }
        };

        _stockRepositoryMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Stock> { stock });
        _analysisRepositoryMock.Setup(x => x.GetConsecutiveResultsAsync(1, Recommendation.Buy, It.IsAny<DateOnly>(), 3))
            .ReturnsAsync(results);

        var request = new ConsecutiveQueryRequest { Days = 3, Recommendation = Recommendation.Buy };

        // Act
        var result = await _sut.GetConsecutiveStocksAsync(request);

        // Assert
        result.Stocks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCorrectStats()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.GetActiveCountAsync()).ReturnsAsync(5);
        _analysisRepositoryMock.Setup(x => x.GetStatisticsInfoAsync())
            .ReturnsAsync((100, DateOnly.Parse("2026-01-01"), DateOnly.Parse("2026-01-30")));
        _analysisRepositoryMock.Setup(x => x.GetRecommendationStatsAsync())
            .ReturnsAsync(new Dictionary<Recommendation, (int Count, decimal AvgConfidence)>
            {
                { Recommendation.Buy, (40, 75.5m) },
                { Recommendation.Hold, (35, 60.0m) },
                { Recommendation.Sell, (25, 70.0m) }
            });

        // Act
        var result = await _sut.GetSummaryAsync();

        // Assert
        result.TotalStocks.Should().Be(5);
        result.TotalAnalysis.Should().Be(100);
        result.BuySummary.Count.Should().Be(40);
        result.BuySummary.Percentage.Should().Be(40);
        result.HoldSummary.Count.Should().Be(35);
        result.SellSummary.Count.Should().Be(25);
    }

    [Fact]
    public async Task GetStockTrendAsync_WhenStockNotFound_ShouldReturnNull()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.GetBySymbolAsync("AAPL")).ReturnsAsync((Stock?)null);

        // Act
        var result = await _sut.GetStockTrendAsync("AAPL");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStockTrendAsync_WhenNoResults_ShouldReturnEmptyTrend()
    {
        // Arrange
        var stock = new Stock { Id = 1, Symbol = "AAPL", Name = "Apple Inc." };
        _stockRepositoryMock.Setup(x => x.GetBySymbolAsync("AAPL")).ReturnsAsync(stock);
        _analysisRepositoryMock.Setup(x => x.GetByStockIdAsync(1, 30)).ReturnsAsync(new List<AnalysisResult>());

        // Act
        var result = await _sut.GetStockTrendAsync("AAPL");

        // Assert
        result.Should().NotBeNull();
        result!.TrendData.Should().BeEmpty();
        result.Summary.TotalDays.Should().Be(0);
        result.Summary.DominantRecommendation.Should().Be("N/A");
    }

    [Fact]
    public async Task GetStockTrendAsync_ShouldCalculateCorrectDominant()
    {
        // Arrange
        var stock = new Stock { Id = 1, Symbol = "AAPL", Name = "Apple Inc." };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var results = new List<AnalysisResult>
        {
            new() { Id = 5, StockId = 1, Stock = stock, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Test" },
            new() { Id = 4, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Buy, Confidence = 80, Reasoning = "Test" },
            new() { Id = 3, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-2), Recommendation = Recommendation.Buy, Confidence = 75, Reasoning = "Test" },
            new() { Id = 2, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-3), Recommendation = Recommendation.Hold, Confidence = 60, Reasoning = "Test" },
            new() { Id = 1, StockId = 1, Stock = stock, AnalysisDate = today.AddDays(-4), Recommendation = Recommendation.Sell, Confidence = 70, Reasoning = "Test" }
        };

        _stockRepositoryMock.Setup(x => x.GetBySymbolAsync("AAPL")).ReturnsAsync(stock);
        _analysisRepositoryMock.Setup(x => x.GetByStockIdAsync(1, 30)).ReturnsAsync(results);

        // Act
        var result = await _sut.GetStockTrendAsync("AAPL");

        // Assert
        result.Should().NotBeNull();
        result!.Summary.TotalDays.Should().Be(5);
        result.Summary.BuyDays.Should().Be(3);
        result.Summary.HoldDays.Should().Be(1);
        result.Summary.SellDays.Should().Be(1);
        result.Summary.DominantRecommendation.Should().Be("Buy");
    }
}
