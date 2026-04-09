using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Core.Services;
using Xunit;

namespace StockAnalyzer.Tests.Unit.Services;

public class StockServiceTests
{
    private readonly Mock<IStockRepository> _stockRepositoryMock;
    private readonly Mock<IAnalysisRepository> _analysisRepositoryMock;
    private readonly Mock<ILogger<StockService>> _loggerMock;
    private readonly StockService _sut;

    public StockServiceTests()
    {
        _stockRepositoryMock = new Mock<IStockRepository>();
        _analysisRepositoryMock = new Mock<IAnalysisRepository>();
        _loggerMock = new Mock<ILogger<StockService>>();
        
        _sut = new StockService(
            _stockRepositoryMock.Object,
            _analysisRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AddStockAsync_WhenStockNotExists_ShouldAddSuccessfully()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "AAPL", Name = "Apple Inc." };
        _stockRepositoryMock.Setup(x => x.ExistsAsync("AAPL")).ReturnsAsync(false);
        _stockRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Stock>()))
            .ReturnsAsync((Stock s) => { s.Id = 1; return s; });

        // Act
        var result = await _sut.AddStockAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Symbol.Should().Be("AAPL");
        result.Name.Should().Be("Apple Inc.");
        _stockRepositoryMock.Verify(x => x.AddAsync(It.Is<Stock>(s => s.Symbol == "AAPL")), Times.Once);
    }

    [Fact]
    public async Task AddStockAsync_WhenStockExists_ShouldThrowException()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "AAPL", Name = "Apple Inc." };
        _stockRepositoryMock.Setup(x => x.ExistsAsync("AAPL")).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddStockAsync(request));
    }

    [Fact]
    public async Task AddStockAsync_ShouldNormalizeSymbolToUpperCase()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "aapl", Name = "Apple Inc." };
        _stockRepositoryMock.Setup(x => x.ExistsAsync("AAPL")).ReturnsAsync(false);
        _stockRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Stock>()))
            .ReturnsAsync((Stock s) => { s.Id = 1; return s; });

        // Act
        var result = await _sut.AddStockAsync(request);

        // Assert
        result.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetStockBySymbolAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.GetBySymbolAsync("AAPL")).ReturnsAsync((Stock?)null);

        // Act
        var result = await _sut.GetStockBySymbolAsync("AAPL");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllStocksAsync_ShouldReturnAllStocks()
    {
        // Arrange
        var stocks = new List<Stock>
        {
            new() { Id = 1, Symbol = "AAPL", Name = "Apple Inc.", IsActive = true },
            new() { Id = 2, Symbol = "GOOGL", Name = "Alphabet Inc.", IsActive = true }
        };
        _stockRepositoryMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(stocks);
        _analysisRepositoryMock.Setup(x => x.GetByStockIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<AnalysisResult>());

        // Act
        var result = await _sut.GetAllStocksAsync();

        // Assert
        result.Stocks.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task DeleteStockAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.DeleteAsync("AAPL")).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteStockAsync("AAPL");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteStockAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.DeleteAsync("AAPL")).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteStockAsync("AAPL");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStockAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        _stockRepositoryMock.Setup(x => x.GetBySymbolAsync("AAPL")).ReturnsAsync((Stock?)null);

        // Act
        var result = await _sut.UpdateStockAsync("AAPL", new UpdateStockRequest { Name = "New Name" });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStockAsync_WhenExists_ShouldUpdateAndReturn()
    {
        // Arrange
        var existingStock = new Stock { Id = 1, Symbol = "AAPL", Name = "Apple Inc.", IsActive = true };
        _stockRepositoryMock.Setup(x => x.GetBySymbolAsync("AAPL")).ReturnsAsync(existingStock);
        _stockRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Stock>())).ReturnsAsync((Stock s) => s);
        _analysisRepositoryMock.Setup(x => x.GetByStockIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<AnalysisResult>());

        // Act
        var result = await _sut.UpdateStockAsync("AAPL", new UpdateStockRequest { Name = "Apple Corporation" });

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Apple Corporation");
    }
}
