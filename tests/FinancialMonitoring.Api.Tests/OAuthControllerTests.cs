using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Controllers.V2;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancialMonitoring.Api.Tests;

public class OAuthControllerTests
{
    private readonly OAuthController _controller;
    private readonly Mock<IOAuthClientService> _mockOAuthClientService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<OAuthController>> _mockLogger;

    public OAuthControllerTests()
    {
        _mockOAuthClientService = new Mock<IOAuthClientService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockLogger = new Mock<ILogger<OAuthController>>();
        
        _controller = new OAuthController(
            _mockOAuthClientService.Object,
            _mockJwtTokenService.Object,
            _mockLogger.Object);
        
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Token_WithValidClientCredentials_ReturnsTokenResponse()
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Scope = "read write"
        };

        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client-id",
            Name = "Test Client",
            AllowedScopes = "read,write,analytics",
            IsActive = true
        };

        var grantedScopes = new[] { "read", "write" };
        var accessToken = "test-access-token";
        var expiresInSeconds = 3600;

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(grantedScopes);

        _mockJwtTokenService
            .Setup(s => s.GenerateClientAccessToken(testClient, grantedScopes))
            .Returns(accessToken);

        _mockJwtTokenService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(expiresInSeconds);

        var result = await _controller.Token(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
        
        Assert.Equal(accessToken, tokenResponse.AccessToken);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.Equal(expiresInSeconds, tokenResponse.ExpiresIn);
        Assert.Equal("read write", tokenResponse.Scope);

        _mockOAuthClientService.Verify(s => s.UpdateLastUsedAsync(testClient.ClientId), Times.Once);
    }

    [Fact]
    public async Task Token_WithInvalidGrantType_ReturnsUnsupportedGrantTypeError()
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "password",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        var result = await _controller.Token(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<OAuthErrorResponse>(badRequestResult.Value);
        
        Assert.Equal("unsupported_grant_type", errorResponse.Error);
        Assert.Contains("password", errorResponse.ErrorDescription);
    }

    [Fact]
    public async Task Token_WithInvalidClientCredentials_ReturnsInvalidClientError()
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "client_credentials",
            ClientId = "invalid-client",
            ClientSecret = "invalid-secret"
        };

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync((OAuthClient?)null);

        var result = await _controller.Token(request);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var errorResponse = Assert.IsType<OAuthErrorResponse>(unauthorizedResult.Value);
        
        Assert.Equal("invalid_client", errorResponse.Error);
    }

    [Fact]
    public async Task Token_WithRequestedScopesNotGranted_ReturnsInvalidScopeError()
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Scope = "admin"
        };

        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client-id",
            Name = "Test Client",
            AllowedScopes = "read,write",
            IsActive = true
        };

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(Enumerable.Empty<string>());

        var result = await _controller.Token(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<OAuthErrorResponse>(badRequestResult.Value);
        
        Assert.Equal("invalid_scope", errorResponse.Error);
    }

    [Fact]
    public async Task Token_WithNoRequestedScopes_GrantsAllAllowedScopes()
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
            // No scope specified
        };

        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client-id",
            Name = "Test Client",
            AllowedScopes = "read,write,analytics",
            IsActive = true
        };

        var allScopes = new[] { "read", "write", "analytics" };
        var accessToken = "test-access-token";

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(allScopes);

        _mockJwtTokenService
            .Setup(s => s.GenerateClientAccessToken(testClient, allScopes))
            .Returns(accessToken);

        _mockJwtTokenService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        var result = await _controller.Token(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
        
        Assert.Equal("read write analytics", tokenResponse.Scope);
    }

    [Fact]
    public async Task Token_WhenExceptionThrown_ReturnsInternalServerError()
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var result = await _controller.Token(request);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<OAuthErrorResponse>(statusCodeResult.Value);
        Assert.Equal("invalid_request", errorResponse.Error);
    }
}