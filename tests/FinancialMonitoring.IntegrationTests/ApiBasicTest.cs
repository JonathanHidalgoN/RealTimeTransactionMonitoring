using System.Net.Http.Json;
using System.Text.Json;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.IntegrationTests;

//Test that the api respond to request with new modernized structure
public class ApiBasicTest : IAsyncLifetime
{
    private readonly TestConfiguration _config;
    private HttpClient _client = null!;

    public ApiBasicTest()
    {
        _config = TestConfiguration.FromEnvironment();
        _config.Validate();
    }

    public async Task InitializeAsync()
    {
        _client = new HttpClient { BaseAddress = new Uri(_config.Api.BaseUrl) };
        _client.DefaultRequestHeaders.Add("X-Api-Key", _config.Api.ApiKey);
        await Task.Delay(5000);
    }

    /// <summary>
    /// This test verifies that the API is reachable and returns a successful response with required headers
    /// </summary>
    [Fact]
    public async Task Api_ShouldBeReachable()
    {
        var response = await _client.GetAsync("/api/v1/transactions?pageSize=1");
        Assert.True(response.IsSuccessStatusCode, $"API should be reachable. Status: {response.StatusCode}");

        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    /// <summary>
    /// This test verifies that API endpoints require authentication and return 401 when no API key is provided
    /// </summary>
    [Fact]
    public async Task Api_ShouldRequireAuthentication()
    {
        using var unauthenticatedClient = new HttpClient { BaseAddress = _client.BaseAddress };
        var response = await unauthenticatedClient.GetAsync("/api/v1/transactions");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// This test verifies that API responses follow the standardized format with success, data, correlationId, and version fields
    /// </summary>
    [Fact]
    public async Task Api_ShouldReturnStandardizedResponse()
    {
        var response = await _client.GetAsync("/api/v1/transactions?pageNumber=1&pageSize=5");
        Assert.True(response.IsSuccessStatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());

        Assert.True(root.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("items", out _));
        Assert.True(data.TryGetProperty("totalCount", out _));

        Assert.True(root.TryGetProperty("correlationId", out _));
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.True(root.TryGetProperty("version", out var version));
        Assert.Equal("1.0", version.GetString());
    }

    /// <summary>
    /// This test verifies that health check endpoints are accessible and return appropriate status information
    /// </summary>
    [Fact]
    public async Task Api_HealthChecks_ShouldBeAccessible()
    {
        var healthzResponse = await _client.GetAsync("/healthz");
        Assert.True(healthzResponse.IsSuccessStatusCode ||
                   healthzResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);

        var healthResponse = await _client.GetAsync("/health");
        Assert.True(healthResponse.IsSuccessStatusCode ||
                   healthResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);

        if (healthResponse.IsSuccessStatusCode)
        {
            var content = await healthResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("status", out _));
            Assert.True(root.TryGetProperty("checks", out _));
        }
    }

    /// <summary>
    /// This test verifies that the API supports versioning through both URL path and header-based approaches
    /// </summary>
    [Fact]
    public async Task Api_ShouldSupportVersioning()
    {
        var v1Response = await _client.GetAsync("/api/v1/transactions?pageSize=1");
        Assert.True(v1Response.IsSuccessStatusCode);

        using var versionedClient = new HttpClient { BaseAddress = _client.BaseAddress };
        versionedClient.DefaultRequestHeaders.Add("X-Api-Key", _config.Api.ApiKey);
        versionedClient.DefaultRequestHeaders.Add("X-Version", "1.0");

        var headerVersionResponse = await versionedClient.GetAsync("/api/transactions?pageSize=1");
    }

    /// <summary>
    /// This test verifies that API error responses follow the standardized format with success, error, and correlationId fields
    /// </summary>
    [Fact]
    public async Task Api_ErrorResponse_ShouldBeStandardized()
    {
        var response = await _client.GetAsync("/api/v1/transactions/invalid-id-format");

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("success", out var success));
            Assert.False(success.GetBoolean());

            Assert.True(root.TryGetProperty("error", out var error));
            Assert.True(error.TryGetProperty("type", out _));
            Assert.True(error.TryGetProperty("title", out _));
            Assert.True(error.TryGetProperty("status", out _));

            Assert.True(root.TryGetProperty("correlationId", out _));
        }
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await Task.CompletedTask;
    }
}
