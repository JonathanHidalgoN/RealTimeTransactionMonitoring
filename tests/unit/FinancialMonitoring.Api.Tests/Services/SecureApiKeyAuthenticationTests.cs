using System.Net;
using FinancialMonitoring.Models;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Tests.Infrastructure;
using Moq;

namespace FinancialMonitoring.Api.Tests.Services;

/// <summary>
/// Tests for the secure API key authentication system
/// </summary>
public class SecureApiKeyAuthenticationTests : IClassFixture<SharedWebApplicationFactory>
{
    private readonly SharedWebApplicationFactory _factory;

    public SecureApiKeyAuthenticationTests(SharedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Request_WithValidApiKey_ShouldSucceed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, SharedWebApplicationFactory.TestApiKey);

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20
        };

        _factory.MockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 20))
            .ReturnsAsync(expectedPagedResult);

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));
    }

    [Fact]
    public async Task Request_WithNoApiKey_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithInvalidApiKey_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "invalid-key");

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithEmptyApiKey_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "");

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithVeryLongApiKey_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var veryLongKey = new string('a', 10000);
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, veryLongKey);

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithSpecialCharactersInApiKey_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var specialKey = "invalid-key-with-!@#$%^&*()_+-={}[]|:;<>?";
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, specialKey);

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithUnicodeCharactersInApiKey_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var unicodeKey = "invalid-key-with-üñíçødé-🔐";
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, unicodeKey);

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRequests_WithValidApiKey_ShouldAllSucceed()
    {
        const int concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            _factory.MockRepository
                .Setup(service => service.GetAllTransactionsAsync(1, 20))
                .ReturnsAsync(new PagedResult<Transaction>
                {
                    Items = new List<Transaction>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = 20
                });

            tasks.Add(Task.Run(async () =>
            {
                var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, SharedWebApplicationFactory.TestApiKey);
                return await client.GetAsync(AppConstants.Routes.GetTransactionsPath());
            }));
        }

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, response =>
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Theory]
    [InlineData("GET", "transactions")]
    [InlineData("GET", "transactions/test-id")]
    [InlineData("GET", "transactions/anomalies")]
    public async Task AllEndpoints_RequireAuthentication(string method, string endpoint)
    {
        var client = _factory.CreateClient();

        var fullEndpoint = endpoint switch
        {
            "transactions" => AppConstants.Routes.GetTransactionsPath(),
            "transactions/test-id" => AppConstants.Routes.GetTransactionByIdPath("test-id"),
            "transactions/anomalies" => AppConstants.Routes.GetAnomaliesPath(),
            _ => $"{AppConstants.Routes.GetVersionedApiPath()}/{endpoint}"
        };

        var request = new HttpRequestMessage(new HttpMethod(method), fullEndpoint);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidationError_AndValidApiKey_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, SharedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", content.ToLower());
    }
}
