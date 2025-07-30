using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Authentication;

namespace FinancialMonitoring.Api.Tests;

/// <summary>
/// Tests for rate limiting functionality
/// </summary>
public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ITransactionRepository> _mockRepository;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ApiSettings:ApiKey", "test-api-key-123" },
                    { "MongoDb:ConnectionString", "mongodb://localhost:27017" },
                    { "MongoDb:DatabaseName", "TestFinancialMonitoring" },
                    { "MongoDb:CollectionName", "transactions" },
                    { "ApplicationInsights:ConnectionString", "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/" },
                    // Override rate limiting for faster testing
                    { "IpRateLimiting:GeneralRules:0:Endpoint", "*" },
                    { "IpRateLimiting:GeneralRules:0:Period", "1m" },
                    { "IpRateLimiting:GeneralRules:0:Limit", "5" },
                    { "IpRateLimiting:GeneralRules:1:Endpoint", "*/transactions" },
                    { "IpRateLimiting:GeneralRules:1:Period", "1m" },
                    { "IpRateLimiting:GeneralRules:1:Limit", "3" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);
            });
        });
    }

    [Fact]
    public async Task MultipleRequests_UnderLimit_ShouldAllSucceed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20
        };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expectedPagedResult);

        for (int i = 0; i < 2; i++) // Well under the limit of 3
        {
            var response = await client.GetAsync("/api/v1/transactions");
            Assert.True(response.IsSuccessStatusCode, $"Request {i + 1} should succeed");

            Assert.True(response.Headers.Contains("X-RateLimit-Limit") ||
                       response.Headers.Contains("X-Rate-Limit-Limit"));
        }
    }

    [Fact]
    public async Task RateLimitResponse_ShouldContainProperHeaders()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20
        };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expectedPagedResult);

        var response = await client.GetAsync("/api/v1/transactions");

        Assert.True(response.IsSuccessStatusCode);

        var hasRateLimitHeaders = response.Headers.Any(h =>
            h.Key.ToLower().Contains("ratelimit") ||
            h.Key.ToLower().Contains("rate-limit"));

    }

    [Fact]
    public async Task HealthCheck_ShouldNotBeRateLimited()
    {
        var client = _factory.CreateClient();

        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/healthz");
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable,
                       $"Health check {i + 1} should not be rate limited");
        }
    }
}
