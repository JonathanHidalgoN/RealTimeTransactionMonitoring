using System.Net;
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
using FinancialMonitoring.Models.OAuth;

namespace FinancialMonitoring.Api.Tests.WebApi;

/// <summary>
/// Tests for rate limiting functionality including OAuth endpoints
/// </summary>
public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<IOAuthClientService> _mockOAuthService;
    private readonly Mock<IJwtTokenService> _mockJwtService;

    // Essential rate limits from appsettings.Test.json
    private const int OAUTH_TOKEN_RATE_LIMIT = 10;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockOAuthService = new Mock<IOAuthClientService>();
        _mockJwtService = new Mock<IJwtTokenService>();

        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            Name = "Test Client",
            AllowedScopes = "read,write",
            IsActive = true
        };

        _mockOAuthService.Setup(s => s.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testClient);
        _mockOAuthService.Setup(s => s.DetermineGrantedScopes(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read", "write" });
        _mockJwtService.Setup(s => s.GenerateClientAccessToken(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns("test-access-token");
        _mockJwtService.Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                var testConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
                configBuilder.AddJsonFile(testConfigPath, optional: false);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);
                services.RemoveAll<IOAuthClientService>();
                services.AddSingleton<IOAuthClientService>(_mockOAuthService.Object);
                services.RemoveAll<IJwtTokenService>();
                services.AddSingleton<IJwtTokenService>(_mockJwtService.Object);

                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
            });
        });
    }

    private static FormUrlEncodedContent CreateOAuthTokenRequest()
    {
        return new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("GrantType", "client_credentials"),
            new KeyValuePair<string, string>("ClientId", "test-client"),
            new KeyValuePair<string, string>("ClientSecret", "test-secret")
        });
    }

    [Fact]
    public async Task RateLimit_CoreBlocking_Returns429()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", "192.168.1.1");
        var requestContent = CreateOAuthTokenRequest();

        for (int i = 0; i < OAUTH_TOKEN_RATE_LIMIT + 1; i++)
        {
            var response = await client.PostAsync("/api/v2/oauth/token", requestContent);

            if (i < OAUTH_TOKEN_RATE_LIMIT)
            {
                Assert.True(response.IsSuccessStatusCode,
                    $"Request {i + 1} should succeed within rate limit. Status: {response.StatusCode}");
            }
            else
            {
                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                Assert.NotNull(response.Headers.RetryAfter);
            }
        }
    }

    [Fact]
    public async Task RateLimit_DifferentEndpoints_IndependentLimits()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");
        client.DefaultRequestHeaders.Add("X-Real-IP", "192.168.1.2");

        _mockRepository.Setup(s => s.GetAllTransactionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Transaction> { Items = new List<Transaction>(), TotalCount = 0, PageNumber = 1, PageSize = 20 });

        // Test that different endpoints have independent rate limits
        // OAuth tokens (10/min), Transactions (50/min), OAuth clients (20/min) should not interfere

        var transactionResponse = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());
        Assert.True(transactionResponse.IsSuccessStatusCode, "Transactions endpoint should be accessible");

        var clientsResponse = await client.GetAsync("/api/v2/oauth/clients");
        Assert.True(clientsResponse.StatusCode == HttpStatusCode.Unauthorized ||
                   clientsResponse.StatusCode == HttpStatusCode.Forbidden,
                   $"OAuth clients should return auth error, not rate limit. Got: {clientsResponse.StatusCode}");

        var tokenResponse = await client.PostAsync("/api/v2/oauth/token", CreateOAuthTokenRequest());
        Assert.True(tokenResponse.IsSuccessStatusCode, "OAuth token endpoint should be accessible independently");
    }

    [Fact]
    public async Task RateLimit_HealthCheck_AlwaysAllowed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", "192.168.1.3");

        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable,
                       $"Health check {i + 1} should not be rate limited");
        }
    }

    [Fact]
    public async Task RateLimit_InvalidRequests_StillCounted()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", "192.168.1.4");

        _mockOAuthService.Setup(s => s.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((OAuthClient?)null);

        var requestContent = CreateOAuthTokenRequest();

        for (int i = 0; i < OAUTH_TOKEN_RATE_LIMIT + 1; i++)
        {
            var response = await client.PostAsync("/api/v2/oauth/token", requestContent);

            if (i < OAUTH_TOKEN_RATE_LIMIT)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
                Assert.NotNull(response.Headers.RetryAfter);
            }
        }
    }
}
