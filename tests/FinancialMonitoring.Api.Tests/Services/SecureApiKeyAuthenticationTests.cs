using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Services;

namespace FinancialMonitoring.Api.Tests.Services;

/// <summary>
/// Tests for the secure API key authentication system
/// </summary>
public class SecureApiKeyAuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ITransactionRepository> _mockRepository;

    public SecureApiKeyAuthenticationTests(WebApplicationFactory<Program> factory)
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
                    { "JwtSettings:SecretKey", "test-secret-key-that-is-very-long-for-hmac-sha256" },
                    { "JwtSettings:Issuer", "TestIssuer" },
                    { "JwtSettings:Audience", "TestAudience" },
                    { "JwtSettings:ExpiresInMinutes", "15" },
                    { "JwtSettings:RefreshTokenExpiryInDays", "7" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);

                // Add missing authentication services that InMemoryUserRepository needs
                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
                services.RemoveAll<IJwtTokenService>();
                services.AddScoped<IJwtTokenService, JwtTokenService>();

                // Configure JWT options
                services.Configure<JwtSettings>(options =>
                {
                    options.SecretKey = "test-secret-key-that-is-very-long-for-hmac-sha256";
                    options.Issuer = "TestIssuer";
                    options.Audience = "TestAudience";
                    options.AccessTokenExpiryMinutes = 15;
                    options.RefreshTokenExpiryDays = 7;
                });
            });
        });
    }

    [Fact]
    public async Task Request_WithValidApiKey_ShouldSucceed()
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
    public async Task Request_WithCustomCorrelationId_ShouldPreserveIt()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var customCorrelationId = "custom-correlation-123";
        client.DefaultRequestHeaders.Add(AppConstants.CorrelationIdHeader, customCorrelationId);

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 20
        };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 20))
            .ReturnsAsync(expectedPagedResult);

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));

        var correlationIdHeader = response.Headers.GetValues(AppConstants.CorrelationIdHeader).FirstOrDefault();
        Assert.Equal(customCorrelationId, correlationIdHeader);
    }

    [Theory]
    [InlineData("GET", "transactions")]
    [InlineData("GET", "transactions/test-id")]
    [InlineData("GET", "transactions/anomalies")]
    public async Task AllEndpoints_RequireAuthentication(string method, string endpoint)
    {
        var client = _factory.CreateClient();

        // Build full endpoint path using constants
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
}
