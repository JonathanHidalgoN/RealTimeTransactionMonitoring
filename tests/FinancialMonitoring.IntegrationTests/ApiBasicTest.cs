using System.Net.Http.Json;

namespace FinancialMonitoring.IntegrationTests;

//Test that the api respond to request
public class ApiBasicTest : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl") ?? "http://financialmonitoring-api-test:8080";
        var apiKey = Environment.GetEnvironmentVariable("ApiKey") ?? "integration-test-key";
        _client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        await Task.Delay(5000);
    }

    [Fact]
    public async Task Api_ShouldBeReachable()
    {
        var response = await _client.GetAsync("/api/transactions?pageSize=1");
        Assert.True(response.IsSuccessStatusCode, $"API should be reachable. Status: {response.StatusCode}");
    }

    [Fact]
    public async Task Api_ShouldRequireAuthentication()
    {
        using var unauthenticatedClient = new HttpClient { BaseAddress = _client.BaseAddress };
        var response = await unauthenticatedClient.GetAsync("/api/transactions");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_ShouldReturnPagedResults()
    {
        var response = await _client.GetAsync("/api/transactions?pageNumber=1&pageSize=5");
        Assert.True(response.IsSuccessStatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"items\"", content.ToLower());
        Assert.Contains("\"totalcount\"", content.ToLower());
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await Task.CompletedTask;
    }
}
