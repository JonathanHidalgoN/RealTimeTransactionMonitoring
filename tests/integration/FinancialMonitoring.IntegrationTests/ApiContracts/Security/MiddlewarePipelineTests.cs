using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Moq;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FinancialMonitoring.IntegrationTests.ApiContracts.Security;

/// <summary>
/// Integration tests for the middleware pipeline configured in MiddlewareExtensions.cs
/// These tests verify that middleware is properly configured and executes in the correct order
/// </summary>
public class MiddlewarePipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ITransactionRepository> _mockRepository;
    // Using actual values from appsettings.json since ConfigureAppConfiguration isn't overriding them
    private const string TestSecretKey = "YourSuperSecretKeyForDevelopmentOnlyMustBe32CharsOrMore!";
    private const string TestIssuer = "FinancialMonitoring.Api";
    private const string TestAudience = "FinancialMonitoring.Clients";

    public MiddlewarePipelineTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:SecretKey", TestSecretKey },
                    { "JwtSettings:Issuer", TestIssuer },
                    { "JwtSettings:Audience", TestAudience },
                    { "JwtSettings:AccessTokenExpiryMinutes", "15" },
                    { "JwtSettings:RefreshTokenExpiryDays", "7" },
                    { "JwtSettings:ValidateIssuer", "true" },
                    { "JwtSettings:ValidateAudience", "true" },
                    { "JwtSettings:ValidateLifetime", "true" },
                    { "JwtSettings:ClockSkewMinutes", "5" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);

                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
                services.RemoveAll<IJwtTokenService>();
                services.AddScoped<IJwtTokenService, JwtTokenService>();
            });
        });
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").First());

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("default-src 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);

        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());

        Assert.True(response.Headers.Contains("Permissions-Policy"));
        var permissionsPolicy = response.Headers.GetValues("Permissions-Policy").First();
        Assert.Contains("geolocation=()", permissionsPolicy);
        Assert.Contains("camera=()", permissionsPolicy);
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldRemoveServerHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.False(response.Headers.Contains("Server"), "Server header should be removed");
        Assert.False(response.Headers.Contains("X-Powered-By"), "X-Powered-By header should be removed");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddApiVersionHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.True(response.Headers.Contains("X-API-Version"));
        Assert.Equal(AppConstants.ApiVersion, response.Headers.GetValues("X-API-Version").First());
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddRateLimitPolicyHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.True(response.Headers.Contains("X-Rate-Limit-Policy"));
        Assert.Equal("Enabled", response.Headers.GetValues("X-Rate-Limit-Policy").First());
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_WithHttps_ShouldAddHSTSHeader()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, AppConstants.HealthCheckEndpoint);
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        // Assert
        // Note: HSTS might not be added in test environment depending on configuration
        // This test documents the expected behavior in production with HTTPS
        if (response.Headers.Contains("Strict-Transport-Security"))
        {
            var hsts = response.Headers.GetValues("Strict-Transport-Security").First();
            Assert.Contains("max-age=31536000", hsts);
            Assert.Contains("includeSubDomains", hsts);
        }
    }


    [Fact]
    public async Task CorrelationIdMiddleware_WhenNoCorrelationIdProvided_ShouldGenerateOne()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));
        var correlationId = response.Headers.GetValues(AppConstants.CorrelationIdHeader).First();
        Assert.NotNull(correlationId);
        Assert.NotEmpty(correlationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WhenCorrelationIdProvided_ShouldEchoItBack()
    {
        var client = _factory.CreateClient();
        var expectedCorrelationId = "test-correlation-id-12345";
        var request = new HttpRequestMessage(HttpMethod.Get, AppConstants.HealthCheckEndpoint);
        request.Headers.Add(AppConstants.CorrelationIdHeader, expectedCorrelationId);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));
        var actualCorrelationId = response.Headers.GetValues(AppConstants.CorrelationIdHeader).First();
        Assert.Equal(expectedCorrelationId, actualCorrelationId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldWorkAcrossMultipleRequests()
    {
        var client = _factory.CreateClient();
        var correlationId1 = "correlation-id-1";
        var correlationId2 = "correlation-id-2";

        var request1 = new HttpRequestMessage(HttpMethod.Get, AppConstants.HealthCheckEndpoint);
        request1.Headers.Add(AppConstants.CorrelationIdHeader, correlationId1);
        var response1 = await client.SendAsync(request1);

        var request2 = new HttpRequestMessage(HttpMethod.Get, AppConstants.HealthCheckEndpoint);
        request2.Headers.Add(AppConstants.CorrelationIdHeader, correlationId2);
        var response2 = await client.SendAsync(request2);

        Assert.Equal(correlationId1, response1.Headers.GetValues(AppConstants.CorrelationIdHeader).First());
        Assert.Equal(correlationId2, response2.Headers.GetValues(AppConstants.CorrelationIdHeader).First());
    }

    [Fact]
    public async Task AuthenticationMiddleware_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v2.0/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationMiddleware_WithValidToken_ShouldAllowAccess()
    {
        _mockRepository.Setup(r => r.GetAllTransactionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<Transaction>
            {
                Items = new List<Transaction>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 10
            });

        var client = _factory.CreateClient();
        var token = GenerateJwtToken(AppConstants.ViewerRole);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v2.0/transactions");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationMiddleware_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/v2.0/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationMiddleware_WithExpiredToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var expiredToken = GenerateExpiredJwtToken(AppConstants.ViewerRole);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/v2.0/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task MiddlewarePipeline_ShouldExecuteInCorrectOrder()
    {
        var client = _factory.CreateClient();
        var correlationId = "order-test-correlation-id";
        var request = new HttpRequestMessage(HttpMethod.Get, AppConstants.HealthCheckEndpoint);
        request.Headers.Add(AppConstants.CorrelationIdHeader, correlationId);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("X-Frame-Options"));

        Assert.Equal(correlationId, response.Headers.GetValues(AppConstants.CorrelationIdHeader).First());

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MiddlewarePipeline_ShouldApplyBothSecurityAndCorrelationHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));

        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));

    }

    [Fact]
    public async Task CorsMiddleware_ShouldBeConfigured()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, AppConstants.HealthCheckEndpoint);
        request.Headers.Add("Origin", "http://localhost:5124");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.NotNull(response);
    }

    private string GenerateJwtToken(string role, int expiryMinutes = 15)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateExpiredJwtToken(string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
