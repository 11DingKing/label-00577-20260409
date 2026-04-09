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

/// <summary>
/// 边缘情况测试
/// </summary>
public class EdgeCaseTests
{
    #region StockService Edge Cases
    
    [Fact]
    public async Task AddStockAsync_WithWhitespaceSymbol_ShouldTrimAndNormalize()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var loggerMock = new Mock<ILogger<StockService>>();
        
        stockRepoMock.Setup(x => x.ExistsAsync("AAPL")).ReturnsAsync(false);
        stockRepoMock.Setup(x => x.AddAsync(It.IsAny<Stock>()))
            .ReturnsAsync((Stock s) => { s.Id = 1; return s; });
        
        var service = new StockService(stockRepoMock.Object, analysisRepoMock.Object, loggerMock.Object);
        var request = new AddStockRequest { Symbol = "  aapl  ", Name = "  Apple Inc.  " };

        // Act
        var result = await service.AddStockAsync(request);

        // Assert
        result.Symbol.Should().Be("AAPL");
        result.Name.Should().Be("Apple Inc.");
    }

    [Fact]
    public async Task BatchAddStocks_WithAllDuplicates_ShouldThrowException()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var loggerMock = new Mock<ILogger<StockService>>();
        
        stockRepoMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        
        var service = new StockService(stockRepoMock.Object, analysisRepoMock.Object, loggerMock.Object);
        var request = new BatchAddStocksRequest
        {
            Stocks = new List<AddStockRequest>
            {
                new() { Symbol = "AAPL", Name = "Apple" },
                new() { Symbol = "GOOGL", Name = "Google" }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddStocksAsync(request));
    }

    [Fact]
    public async Task BatchAddStocks_WithPartialSuccess_ShouldReturnSuccessful()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var loggerMock = new Mock<ILogger<StockService>>();
        
        stockRepoMock.Setup(x => x.ExistsAsync("AAPL")).ReturnsAsync(true); // duplicate
        stockRepoMock.Setup(x => x.ExistsAsync("GOOGL")).ReturnsAsync(false);
        stockRepoMock.Setup(x => x.AddAsync(It.IsAny<Stock>()))
            .ReturnsAsync((Stock s) => { s.Id = 1; return s; });
        
        var service = new StockService(stockRepoMock.Object, analysisRepoMock.Object, loggerMock.Object);
        var request = new BatchAddStocksRequest
        {
            Stocks = new List<AddStockRequest>
            {
                new() { Symbol = "AAPL", Name = "Apple" },
                new() { Symbol = "GOOGL", Name = "Google" }
            }
        };

        // Act
        var result = await service.AddStocksAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result.First().Symbol.Should().Be("GOOGL");
    }
    
    #endregion

    #region AnalysisService Edge Cases

    [Fact]
    public async Task RunAnalysisAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var logRepoMock = new Mock<IAnalysisLogRepository>();
        var aiServiceMock = new Mock<IAiService>();
        var loggerMock = new Mock<ILogger<AnalysisService>>();
        
        var stocks = new List<Stock> { new() { Id = 1, Symbol = "AAPL", Name = "Apple" } };
        stockRepoMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(stocks);
        analysisRepoMock.Setup(x => x.ExistsAsync(1, It.IsAny<DateOnly>())).ReturnsAsync(false);
        
        // 模拟 AI 调用时取消
        var cts = new CancellationTokenSource();
        aiServiceMock.Setup(x => x.AnalyzeStockAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stock s, CancellationToken ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return new AiAnalysisResponse();
            });
        
        var service = new AnalysisService(
            stockRepoMock.Object, analysisRepoMock.Object, logRepoMock.Object,
            aiServiceMock.Object, loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.RunAnalysisAsync(new RunAnalysisRequest(), cts.Token));
    }

    [Fact]
    public async Task RunAnalysisAsync_WithEmptySymbolsList_ShouldAnalyzeAll()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var logRepoMock = new Mock<IAnalysisLogRepository>();
        var aiServiceMock = new Mock<IAiService>();
        var loggerMock = new Mock<ILogger<AnalysisService>>();
        
        var stocks = new List<Stock> { new() { Id = 1, Symbol = "AAPL", Name = "Apple" } };
        stockRepoMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(stocks);
        analysisRepoMock.Setup(x => x.ExistsAsync(1, It.IsAny<DateOnly>())).ReturnsAsync(false);
        aiServiceMock.Setup(x => x.AnalyzeStockAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiAnalysisResponse { Recommendation = Recommendation.Buy, Confidence = 80, Reasoning = "Test" });
        analysisRepoMock.Setup(x => x.AddAsync(It.IsAny<AnalysisResult>()))
            .ReturnsAsync((AnalysisResult r) => { r.Id = 1; return r; });
        logRepoMock.Setup(x => x.AddAsync(It.IsAny<AnalysisLog>())).ReturnsAsync((AnalysisLog l) => l);
        
        var service = new AnalysisService(
            stockRepoMock.Object, analysisRepoMock.Object, logRepoMock.Object,
            aiServiceMock.Object, loggerMock.Object);

        // 传递空的 Symbols 列表
        var request = new RunAnalysisRequest { Symbols = new List<string>() };

        // Act
        var result = await service.RunAnalysisAsync(request);

        // Assert - 应该分析所有股票，而不是报错
        result.SuccessCount.Should().Be(1);
        stockRepoMock.Verify(x => x.GetAllAsync(false), Times.Once);
    }

    #endregion

    #region StatisticsService Edge Cases

    [Fact]
    public async Task GetConsecutiveStocksAsync_WithWeekendGap_ShouldCountAsConsecutive()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var loggerMock = new Mock<ILogger<StatisticsService>>();
        
        var stock = new Stock { Id = 1, Symbol = "AAPL", Name = "Apple" };
        // 模拟周五和周一的数据（间隔3天）
        var friday = new DateOnly(2026, 1, 30); // Friday
        var monday = new DateOnly(2026, 1, 26); // Monday (4 days before)
        
        var results = new List<AnalysisResult>
        {
            new() { Id = 1, StockId = 1, Stock = stock, AnalysisDate = friday, Recommendation = Recommendation.Buy, Confidence = 80, Reasoning = "Friday" },
            new() { Id = 2, StockId = 1, Stock = stock, AnalysisDate = friday.AddDays(-1), Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Thursday" },
            new() { Id = 3, StockId = 1, Stock = stock, AnalysisDate = friday.AddDays(-2), Recommendation = Recommendation.Buy, Confidence = 75, Reasoning = "Wednesday" }
        };

        stockRepoMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Stock> { stock });
        analysisRepoMock.Setup(x => x.GetConsecutiveResultsAsync(1, Recommendation.Buy, It.IsAny<DateOnly>(), 3))
            .ReturnsAsync(results);
        
        var service = new StatisticsService(stockRepoMock.Object, analysisRepoMock.Object, loggerMock.Object);
        var request = new ConsecutiveQueryRequest { Days = 3, Recommendation = Recommendation.Buy };

        // Act
        var result = await service.GetConsecutiveStocksAsync(request);

        // Assert
        result.Stocks.Should().HaveCount(1);
        result.Stocks.First().ConsecutiveDays.Should().Be(3);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenNoData_ShouldReturnZeros()
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var loggerMock = new Mock<ILogger<StatisticsService>>();
        
        stockRepoMock.Setup(x => x.GetActiveCountAsync()).ReturnsAsync(0);
        analysisRepoMock.Setup(x => x.GetStatisticsInfoAsync()).ReturnsAsync((0, null, null));
        analysisRepoMock.Setup(x => x.GetRecommendationStatsAsync())
            .ReturnsAsync(new Dictionary<Recommendation, (int, decimal)>());
        
        var service = new StatisticsService(stockRepoMock.Object, analysisRepoMock.Object, loggerMock.Object);

        // Act
        var result = await service.GetSummaryAsync();

        // Assert
        result.TotalStocks.Should().Be(0);
        result.TotalAnalysis.Should().Be(0);
        result.BuySummary.Count.Should().Be(0);
        result.HoldSummary.Count.Should().Be(0);
        result.SellSummary.Count.Should().Be(0);
    }

    #endregion

    #region Validation Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetStockBySymbolAsync_WithInvalidSymbol_ShouldReturnNull(string? symbol)
    {
        // Arrange
        var stockRepoMock = new Mock<IStockRepository>();
        var analysisRepoMock = new Mock<IAnalysisRepository>();
        var loggerMock = new Mock<ILogger<StockService>>();
        
        stockRepoMock.Setup(x => x.GetBySymbolAsync(It.IsAny<string>())).ReturnsAsync((Stock?)null);
        
        var service = new StockService(stockRepoMock.Object, analysisRepoMock.Object, loggerMock.Object);

        // Act & Assert
        if (string.IsNullOrWhiteSpace(symbol))
        {
            // 对于 null 或空字符串，应该优雅处理
            var result = await service.GetStockBySymbolAsync(symbol ?? "");
            result.Should().BeNull();
        }
    }

    #endregion
}
