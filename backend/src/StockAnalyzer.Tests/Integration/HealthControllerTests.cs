using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockAnalyzer.Tests.Integration;

public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Ready_ShouldCheckDatabaseConnection()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Ready");
    }
}
