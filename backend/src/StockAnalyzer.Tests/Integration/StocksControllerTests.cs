using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StockAnalyzer.Core.DTOs;
using Xunit;

namespace StockAnalyzer.Tests.Integration;

public class StocksControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StocksControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WhenEmpty_ShouldReturnEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StockListResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Stocks.Should().BeEmpty();
    }

    [Fact]
    public async Task AddStock_ShouldReturnCreated()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "TEST1", Name = "Test Stock 1" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/stocks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StockResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Symbol.Should().Be("TEST1");
    }

    [Fact]
    public async Task AddStock_WhenDuplicate_ShouldReturnConflict()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "DUPE", Name = "Duplicate Stock" };
        await _client.PostAsJsonAsync("/api/stocks", request);

        // Act
        var response = await _client.PostAsJsonAsync("/api/stocks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddStock_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "", Name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/stocks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBySymbol_WhenExists_ShouldReturnStock()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "GET1", Name = "Get Test Stock" };
        await _client.PostAsJsonAsync("/api/stocks", request);

        // Act
        var response = await _client.GetAsync("/api/stocks/GET1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StockResponse>>();
        result!.Data!.Symbol.Should().Be("GET1");
    }

    [Fact]
    public async Task GetBySymbol_WhenNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/stocks/NOTEXIST");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteStock_WhenExists_ShouldReturnOk()
    {
        // Arrange
        var request = new AddStockRequest { Symbol = "DEL1", Name = "Delete Test Stock" };
        await _client.PostAsJsonAsync("/api/stocks", request);

        // Act
        var response = await _client.DeleteAsync("/api/stocks/DEL1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted
        var getResponse = await _client.GetAsync("/api/stocks/DEL1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BatchAddStocks_ShouldAddMultipleStocks()
    {
        // Arrange
        var request = new BatchAddStocksRequest
        {
            Stocks = new List<AddStockRequest>
            {
                new() { Symbol = "BAT1", Name = "Batch Stock 1" },
                new() { Symbol = "BAT2", Name = "Batch Stock 2" },
                new() { Symbol = "BAT3", Name = "Batch Stock 3" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/stocks/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<StockResponse>>>();
        result!.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateStock_ShouldUpdateAndReturn()
    {
        // Arrange
        var addRequest = new AddStockRequest { Symbol = "UPD1", Name = "Update Test Stock" };
        await _client.PostAsJsonAsync("/api/stocks", addRequest);

        var updateRequest = new UpdateStockRequest { Name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/stocks/UPD1", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<StockResponse>>();
        result!.Data!.Name.Should().Be("Updated Name");
    }
}
