using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Infrastructure.Data;
using StockAnalyzer.Infrastructure.Repositories;
using Xunit;

namespace StockAnalyzer.Tests.Unit.Repositories;

public class StockRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly StockRepository _sut;

    public StockRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new AppDbContext(options);
        _sut = new StockRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddStock()
    {
        // Arrange
        var stock = new Stock { Symbol = "AAPL", Name = "Apple Inc." };

        // Act
        var result = await _sut.AddAsync(stock);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetBySymbolAsync_WhenExists_ShouldReturnStock()
    {
        // Arrange
        var stock = new Stock { Symbol = "GOOGL", Name = "Alphabet Inc." };
        await _sut.AddAsync(stock);

        // Act
        var result = await _sut.GetBySymbolAsync("GOOGL");

        // Assert
        result.Should().NotBeNull();
        result!.Symbol.Should().Be("GOOGL");
    }

    [Fact]
    public async Task GetBySymbolAsync_WhenNotExists_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetBySymbolAsync("NOTEXIST");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveStocks()
    {
        // Arrange
        await _sut.AddAsync(new Stock { Symbol = "ACTIVE1", Name = "Active 1", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "ACTIVE2", Name = "Active 2", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "INACTIVE", Name = "Inactive", IsActive = false });

        // Act
        var result = await _sut.GetAllAsync(includeInactive: false);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ShouldReturnAllStocks()
    {
        // Arrange
        await _sut.AddAsync(new Stock { Symbol = "ACTIVE1", Name = "Active 1", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "INACTIVE", Name = "Inactive", IsActive = false });

        // Act
        var result = await _sut.GetAllAsync(includeInactive: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        await _sut.AddAsync(new Stock { Symbol = "DELETE", Name = "To Delete" });

        // Act
        var result = await _sut.DeleteAsync("DELETE");

        // Assert
        result.Should().BeTrue();
        (await _sut.GetBySymbolAsync("DELETE")).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.DeleteAsync("NOTEXIST");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        await _sut.AddAsync(new Stock { Symbol = "EXISTS", Name = "Exists" });

        // Act
        var result = await _sut.ExistsAsync("EXISTS");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.ExistsAsync("NOTEXISTS");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _sut.AddAsync(new Stock { Symbol = "A1", Name = "Active 1", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "A2", Name = "Active 2", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "I1", Name = "Inactive 1", IsActive = false });

        // Act
        var result = await _sut.GetActiveCountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetBySymbolsAsync_ShouldReturnMatchingStocks()
    {
        // Arrange
        await _sut.AddAsync(new Stock { Symbol = "AAPL", Name = "Apple", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "GOOGL", Name = "Google", IsActive = true });
        await _sut.AddAsync(new Stock { Symbol = "MSFT", Name = "Microsoft", IsActive = true });

        // Act
        var result = await _sut.GetBySymbolsAsync(new[] { "AAPL", "MSFT" });

        // Assert
        result.Should().HaveCount(2);
        result.Select(s => s.Symbol).Should().Contain("AAPL");
        result.Select(s => s.Symbol).Should().Contain("MSFT");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateStock()
    {
        // Arrange
        var stock = new Stock { Symbol = "UPDATE", Name = "Before Update", IsActive = true };
        await _sut.AddAsync(stock);
        
        stock.Name = "After Update";

        // Act
        var result = await _sut.UpdateAsync(stock);

        // Assert
        result.Name.Should().Be("After Update");
        var retrieved = await _sut.GetBySymbolAsync("UPDATE");
        retrieved!.Name.Should().Be("After Update");
    }
}
