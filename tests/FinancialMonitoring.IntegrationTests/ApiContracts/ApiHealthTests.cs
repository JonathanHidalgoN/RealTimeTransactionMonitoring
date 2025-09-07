using FinancialMonitoring.Models;

namespace FinancialMonitoring.IntegrationTests.ApiContracts;

/// <summary>
/// API health and availability tests
/// </summary>
[Trait("Category", "API")]
[Trait("Category", "Smoke")]
public class ApiHealthTests : IAsyncLifetime
{
    private readonly TestConfiguration _config;
    private HttpClient _client = null!;

    public ApiHealthTests()
    {
        _config = TestConfiguration.FromEnvironment();
        _config.Validate();
    }

    public async Task InitializeAsync()
    {
        _client = new HttpClient { BaseAddress = new Uri(_config.Api.BaseUrl) };
        _client.DefaultRequestHeaders.Add("X-API-Key", _config.Api.ApiKey);
        await Task.Delay(2000);
    }

    /// <summary>
    /// This test verifies that the API is responding and healthy within the Docker Compose environment
    /// </summary>
    [Fact]
    public async Task HealthCheck_ApiShouldBeResponding()
    {
        var response = await _client.GetAsync("/api/transactions?pageSize=1");
        Assert.True(response.IsSuccessStatusCode, $"API health check failed with status: {response.StatusCode}");
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await Task.CompletedTask;
    }
}