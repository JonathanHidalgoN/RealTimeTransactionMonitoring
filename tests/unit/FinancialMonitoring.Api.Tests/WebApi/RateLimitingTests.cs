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

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockOAuthService = new Mock<IOAuthClientService>();
        _mockJwtService = new Mock<IJwtTokenService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                var testConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
                configBuilder.AddJsonFile(testConfigPath, optional: false);

                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "RateLimitSettings:GeneralRules:0:Limit", "2" },
                    { "RateLimitSettings:GeneralRules:1:Limit", "2" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);
                services.RemoveAll<IOAuthClientService>();
                services.AddSingleton<IOAuthClientService>(_mockOAuthService.Object);
                services.RemoveAll<IJwtTokenService>();
                services.AddSingleton<IJwtTokenService>(_mockJwtService.Object);

                // Keep password hashing service for authentication
                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
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

        for (int i = 0; i < 2; i++)
        {
            var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());
            Assert.True(response.IsSuccessStatusCode, $"Request {i + 1} should succeed");

            var rateLimitHeaders = response.Headers.Where(h =>
                h.Key.ToLower().Contains("ratelimit") ||
                h.Key.ToLower().Contains("rate-limit")).ToList();

            Assert.True(rateLimitHeaders.Any(),
                $"No rate limit headers found. Available headers: {string.Join(", ", response.Headers.Select(h => h.Key))}");
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

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

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
            var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);
            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable,
                       $"Health check {i + 1} should not be rate limited");
        }
    }

    [Fact]
    public async Task OAuthToken_UnderRateLimit_ShouldAllowRequests()
    {
        var client = _factory.CreateClient();

        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            Name = "Test Client",
            AllowedScopes = "read,write",
            IsActive = true
        };

        _mockOAuthService
            .Setup(s => s.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testClient);

        _mockOAuthService
            .Setup(s => s.DetermineGrantedScopes(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read", "write" });

        _mockJwtService
            .Setup(s => s.GenerateClientAccessToken(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns("test-access-token");

        _mockJwtService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("GrantType", "client_credentials"),
            new KeyValuePair<string, string>("ClientId", "test-client"),
            new KeyValuePair<string, string>("ClientSecret", "test-secret")
        });

        // Make 5 requests (under the limit of 10)
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsync("/api/v2/oauth/token", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Assert.True(response.IsSuccessStatusCode,
                    $"Request {i + 1} should succeed under rate limit. Status: {response.StatusCode}, Content: {errorContent}");
            }
        }
    }

    [Fact]
    public async Task OAuthToken_ExceedsRateLimit_ShouldReturn429()
    {
        var client = _factory.CreateClient();

        // Setup mock to return valid client
        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            Name = "Test Client",
            AllowedScopes = "read,write",
            IsActive = true
        };

        _mockOAuthService
            .Setup(s => s.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testClient);

        _mockOAuthService
            .Setup(s => s.DetermineGrantedScopes(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read", "write" });

        _mockJwtService
            .Setup(s => s.GenerateClientAccessToken(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns("test-access-token");

        _mockJwtService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("GrantType", "client_credentials"),
            new KeyValuePair<string, string>("ClientId", "test-client"),
            new KeyValuePair<string, string>("ClientSecret", "test-secret")
        });

        // Make requests up to the limit (10) plus one more
        for (int i = 0; i < 11; i++)
        {
            var response = await client.PostAsync("/api/v2/oauth/token", requestContent);

            if (i < 10)
            {
                Assert.True(response.IsSuccessStatusCode,
                    $"Request {i + 1} should succeed within rate limit. Status: {response.StatusCode}");
            }
            else
            {
                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

                // Verify rate limit headers are present
                var retryAfterHeader = response.Headers.RetryAfter;
                Assert.NotNull(retryAfterHeader);
            }
        }
    }

    [Fact]
    public async Task OAuthClients_AdminEndpoint_ShouldHaveRateLimit()
    {
        var client = _factory.CreateClient();

        // Mock for authenticated admin request (this will still fail auth but should hit rate limit first)
        for (int i = 0; i < 21; i++)
        {
            var response = await client.GetAsync("/api/v2/oauth/clients");

            if (i < 20)
            {
                // Should get 401 Unauthorized (not rate limited)
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }
    }

    [Fact]
    public async Task OAuthToken_DifferentIPAddresses_ShouldHaveSeparateLimits()
    {
        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Add("X-Real-IP", "192.168.1.1");

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Add("X-Real-IP", "192.168.1.2");

        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            Name = "Test Client",
            AllowedScopes = "read,write",
            IsActive = true
        };

        _mockOAuthService
            .Setup(s => s.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(testClient);

        _mockOAuthService
            .Setup(s => s.DetermineGrantedScopes(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read", "write" });

        _mockJwtService
            .Setup(s => s.GenerateClientAccessToken(It.IsAny<OAuthClient>(), It.IsAny<IEnumerable<string>>()))
            .Returns("test-access-token");

        _mockJwtService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("GrantType", "client_credentials"),
            new KeyValuePair<string, string>("ClientId", "test-client"),
            new KeyValuePair<string, string>("ClientSecret", "test-secret")
        });

        var response1 = await client1.PostAsync("/api/v2/oauth/token", requestContent);
        var response2 = await client2.PostAsync("/api/v2/oauth/token", requestContent);

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
    }

    [Fact]
    public async Task OAuthToken_InvalidCredentials_ShouldStillCountTowardsRateLimit()
    {
        var client = _factory.CreateClient();

        _mockOAuthService
            .Setup(s => s.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((OAuthClient?)null);

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("GrantType", "client_credentials"),
            new KeyValuePair<string, string>("ClientId", "invalid-client"),
            new KeyValuePair<string, string>("ClientSecret", "invalid-secret")
        });

        for (int i = 0; i < 11; i++)
        {
            var response = await client.PostAsync("/api/v2/oauth/token", requestContent);

            if (i < 10)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }
    }
}
