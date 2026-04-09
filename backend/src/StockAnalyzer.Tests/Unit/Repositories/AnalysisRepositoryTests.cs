using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Infrastructure.Data;
using StockAnalyzer.Infrastructure.Repositories;
using Xunit;

namespace StockAnalyzer.Tests.Unit.Repositories;

public class AnalysisRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AnalysisRepository _sut;
    private readonly StockRepository _stockRepository;

    public AnalysisRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new AppDbContext(options);
        _sut = new AnalysisRepository(_context);
        _stockRepository = new StockRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Stock> CreateTestStock(string symbol = "TEST")
    {
        var stock = new Stock { Symbol = symbol, Name = $"{symbol} Inc.", IsActive = true };
        return await _stockRepository.AddAsync(stock);
    }

    [Fact]
    public async Task AddAsync_ShouldAddAnalysisResult()
    {
        // Arrange
        var stock = await CreateTestStock();
        var result = new AnalysisResult
        {
            StockId = stock.Id,
            AnalysisDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Recommendation = Recommendation.Buy,
            Confidence = 85,
            Reasoning = "Test reasoning"
        };

        // Act
        var added = await _sut.AddAsync(result);

        // Assert
        added.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByStockIdAsync_ShouldReturnOrderedResults()
    {
        // Arrange
        var stock = await CreateTestStock();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-2), Recommendation = Recommendation.Sell, Confidence = 70, Reasoning = "Old" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "New" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Hold, Confidence = 60, Reasoning = "Mid" });

        // Act
        var results = await _sut.GetByStockIdAsync(stock.Id);

        // Assert
        results.Should().HaveCount(3);
        results.First().AnalysisDate.Should().Be(today); // 最新的在前
    }

    [Fact]
    public async Task GetByStockAndDateAsync_ShouldReturnCorrectResult()
    {
        // Arrange
        var stock = await CreateTestStock();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Today" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Hold, Confidence = 60, Reasoning = "Yesterday" });

        // Act
        var result = await _sut.GetByStockAndDateAsync(stock.Id, today);

        // Assert
        result.Should().NotBeNull();
        result!.Reasoning.Should().Be("Today");
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        var stock = await CreateTestStock();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Test" });

        // Act
        var exists = await _sut.ExistsAsync(stock.Id, today);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Arrange
        var stock = await CreateTestStock();

        // Act
        var exists = await _sut.ExistsAsync(stock.Id, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var stock = await CreateTestStock();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        for (int i = 0; i < 25; i++)
        {
            await _sut.AddAsync(new AnalysisResult
            {
                StockId = stock.Id,
                AnalysisDate = today.AddDays(-i),
                Recommendation = Recommendation.Buy,
                Confidence = 80,
                Reasoning = $"Result {i}"
            });
        }

        // Act
        var (items, total) = await _sut.GetPagedAsync(page: 1, pageSize: 10);

        // Assert
        items.Should().HaveCount(10);
        total.Should().Be(25);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilters_ShouldFilterResults()
    {
        // Arrange
        var stock = await CreateTestStock("FILT");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "Buy" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Sell, Confidence = 70, Reasoning = "Sell" });

        // Act
        var (items, total) = await _sut.GetPagedAsync(page: 1, pageSize: 10, recommendation: Recommendation.Buy);

        // Assert
        items.Should().HaveCount(1);
        items.First().Recommendation.Should().Be(Recommendation.Buy);
    }

    [Fact]
    public async Task GetStatisticsInfoAsync_ShouldReturnCorrectInfo()
    {
        // Arrange
        var stock = await CreateTestStock();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-5), Recommendation = Recommendation.Buy, Confidence = 85, Reasoning = "First" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today, Recommendation = Recommendation.Sell, Confidence = 70, Reasoning = "Last" });

        // Act
        var (total, firstDate, lastDate) = await _sut.GetStatisticsInfoAsync();

        // Assert
        total.Should().Be(2);
        firstDate.Should().Be(today.AddDays(-5));
        lastDate.Should().Be(today);
    }

    [Fact]
    public async Task GetRecommendationStatsAsync_ShouldReturnGroupedStats()
    {
        // Arrange
        var stock = await CreateTestStock();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today, Recommendation = Recommendation.Buy, Confidence = 80, Reasoning = "Buy1" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-1), Recommendation = Recommendation.Buy, Confidence = 90, Reasoning = "Buy2" });
        await _sut.AddAsync(new AnalysisResult { StockId = stock.Id, AnalysisDate = today.AddDays(-2), Recommendation = Recommendation.Sell, Confidence = 70, Reasoning = "Sell1" });

        // Act
        var stats = await _sut.GetRecommendationStatsAsync();

        // Assert
        stats.Should().ContainKey(Recommendation.Buy);
        stats[Recommendation.Buy].Count.Should().Be(2);
        stats[Recommendation.Buy].AvgConfidence.Should().Be(85); // (80+90)/2
        
        stats.Should().ContainKey(Recommendation.Sell);
        stats[Recommendation.Sell].Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateResult()
    {
        // Arrange
        var stock = await CreateTestStock();
        var result = await _sut.AddAsync(new AnalysisResult
        {
            StockId = stock.Id,
            AnalysisDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Recommendation = Recommendation.Hold,
            Confidence = 50,
            Reasoning = "Before update"
        });

        // Act
        result.Recommendation = Recommendation.Buy;
        result.Confidence = 90;
        result.Reasoning = "After update";
        var updated = await _sut.UpdateAsync(result);

        // Assert
        updated.Recommendation.Should().Be(Recommendation.Buy);
        updated.Confidence.Should().Be(90);
        updated.Reasoning.Should().Be("After update");
    }
}
