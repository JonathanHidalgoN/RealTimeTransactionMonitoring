using System.Net;
using FinancialMonitoring.Models;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Tests.Infrastructure;
using Moq;

namespace FinancialMonitoring.Api.Tests.Services;

/// <summary>
/// Tests for security headers middleware functionality
/// </summary>
public class SecurityHeadersTests : IClassFixture<SharedWebApplicationFactory>
{
    private readonly SharedWebApplicationFactory _factory;

    public SecurityHeadersTests(SharedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// This test creates a mocked API client with the security middleware, then sends a GET request and passes if all expected security headers are present
    /// </summary>
    [Fact]
    public async Task SecurityHeaders_ShouldBePresent_OnApiResponses()
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

        // Check all 8 headers that should be present for HTTP requests (HSTS only for HTTPS)
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").First());

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").First());

        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());

        Assert.True(response.Headers.Contains("Permissions-Policy"));
        Assert.Contains("geolocation=()", response.Headers.GetValues("Permissions-Policy").First());

        Assert.True(response.Headers.Contains("X-API-Version"));
        Assert.Equal(AppConstants.ApiVersion, response.Headers.GetValues("X-API-Version").First());

        Assert.True(response.Headers.Contains("X-Rate-Limit-Policy"));
        Assert.Equal("Enabled", response.Headers.GetValues("X-Rate-Limit-Policy").First());

        // HSTS should NOT be present for HTTP requests
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    /// <summary>
    /// This test sends an unauthenticated request and verifies that all security headers are present even on error responses
    /// </summary>
    [Fact]
    public async Task SecurityHeaders_ShouldBePresent_OnErrorResponses()
    {
        // Deliberately not adding API key to trigger 401
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.Routes.GetTransactionsPath());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Verify all security headers are present even on error responses
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        Assert.True(response.Headers.Contains("X-API-Version"));
        Assert.True(response.Headers.Contains("X-Rate-Limit-Policy"));

        // HSTS should NOT be present for HTTP requests
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }
}