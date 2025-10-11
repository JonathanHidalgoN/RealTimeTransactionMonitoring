using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Controllers.V2;
using FinancialMonitoring.Models.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security;

namespace FinancialMonitoring.Api.Tests.V2;

public class OAuthClientBuilder
{
    private readonly OAuthClient _client = new()
    {
        Id = 1,
        ClientId = "test-client-id",
        Name = "Test Client",
        AllowedScopes = "read,write",
        IsActive = true
    };

    public static OAuthClientBuilder Create() => new();

    public OAuthClientBuilder WithClientId(string clientId)
    {
        _client.ClientId = clientId;
        return this;
    }

    public OAuthClientBuilder WithName(string name)
    {
        _client.Name = name;
        return this;
    }

    public OAuthClientBuilder WithAllowedScopes(string scopes)
    {
        _client.AllowedScopes = scopes;
        return this;
    }

    public OAuthClientBuilder AsInactive()
    {
        _client.IsActive = false;
        return this;
    }

    public OAuthClient Build() => _client;
}

public class ClientCredentialsRequestBuilder
{
    private readonly ClientCredentialsRequest _request = new()
    {
        GrantType = "client_credentials",
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret"
    };

    public static ClientCredentialsRequestBuilder Create() => new();

    public ClientCredentialsRequestBuilder WithGrantType(string grantType)
    {
        _request.GrantType = grantType;
        return this;
    }

    public ClientCredentialsRequestBuilder WithClientId(string clientId)
    {
        _request.ClientId = clientId;
        return this;
    }

    public ClientCredentialsRequestBuilder WithClientSecret(string clientSecret)
    {
        _request.ClientSecret = clientSecret;
        return this;
    }

    public ClientCredentialsRequestBuilder WithScope(string scope)
    {
        _request.Scope = scope;
        return this;
    }

    public ClientCredentialsRequest Build() => _request;
}

public class OAuthControllerV2Tests
{
    private readonly OAuthController _controller;
    private readonly Mock<IOAuthClientService> _mockOAuthClientService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<ILogger<OAuthController>> _mockLogger;

    public OAuthControllerV2Tests()
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
        var request = ClientCredentialsRequestBuilder.Create()
            .WithScope("read write")
            .Build();

        var testClient = OAuthClientBuilder.Create()
            .WithAllowedScopes("read,write,analytics")
            .Build();

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
    public async Task Token_WithNoRequestedScopes_GrantsAllAllowedScopes()
    {
        var request = ClientCredentialsRequestBuilder.Create().Build();

        var testClient = OAuthClientBuilder.Create()
            .WithAllowedScopes("read,write,analytics")
            .Build();

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

    [Theory]
    [InlineData("", "valid-secret")]
    [InlineData(null, "valid-secret")]
    [InlineData("valid-client", "")]
    [InlineData("valid-client", null)]
    [InlineData("", "")]
    public async Task Token_WithInvalidClientCredentials_ReturnsUnauthorized(
        string clientId, string clientSecret)
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = "client_credentials",
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(clientId, clientSecret))
            .ReturnsAsync((OAuthClient?)null);

        var result = await _controller.Token(request);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var errorResponse = Assert.IsType<OAuthErrorResponse>(unauthorizedResult.Value);

        Assert.Equal("invalid_client", errorResponse.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("unsupported_grant")]
    [InlineData("authorization_code")]
    public async Task Token_WithInvalidGrantType_ReturnsUnsupportedGrantTypeError(string grantType)
    {
        var request = new ClientCredentialsRequest
        {
            GrantType = grantType,
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        var result = await _controller.Token(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<OAuthErrorResponse>(badRequestResult.Value);

        Assert.Equal("unsupported_grant_type", errorResponse.Error);
        if (!string.IsNullOrEmpty(grantType))
        {
            Assert.Contains(grantType, errorResponse.ErrorDescription);
        }
    }

    [Fact]
    public async Task Token_WithNonExistentClient_ReturnsInvalidClientError()
    {
        var request = ClientCredentialsRequestBuilder.Create()
            .WithClientId("invalid-client")
            .WithClientSecret("invalid-secret")
            .Build();

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync((OAuthClient?)null);

        var result = await _controller.Token(request);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var errorResponse = Assert.IsType<OAuthErrorResponse>(unauthorizedResult.Value);

        Assert.Equal("invalid_client", errorResponse.Error);
    }

    [Fact]
    public async Task Token_WithInactiveClient_ReturnsInvalidClientError()
    {
        var request = ClientCredentialsRequestBuilder.Create()
            .WithClientId("inactive-client-id")
            .Build();

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
        var request = ClientCredentialsRequestBuilder.Create()
            .WithScope("admin")
            .Build();

        var testClient = OAuthClientBuilder.Create()
            .WithAllowedScopes("read,write")
            .Build();

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
    public async Task Token_WhenDatabaseExceptionThrown_ReturnsInternalServerError()
    {
        var request = ClientCredentialsRequestBuilder.Create().Build();

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var result = await _controller.Token(request);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        var errorResponse = Assert.IsType<OAuthErrorResponse>(statusCodeResult.Value);
        Assert.Equal("invalid_request", errorResponse.Error);
        Assert.Contains("internal error", errorResponse.ErrorDescription?.ToLower() ?? string.Empty);
    }

    [Fact]
    public async Task Token_WhenJwtServiceThrowsException_ReturnsInternalServerError()
    {
        var request = ClientCredentialsRequestBuilder.Create().Build();
        var testClient = OAuthClientBuilder.Create().Build();

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read", "write" });

        _mockJwtTokenService
            .Setup(s => s.GenerateClientAccessToken(testClient, It.IsAny<IEnumerable<string>>()))
            .Throws(new SecurityException("Token generation failed"));

        var result = await _controller.Token(request);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        var errorResponse = Assert.IsType<OAuthErrorResponse>(statusCodeResult.Value);
        Assert.Equal("invalid_request", errorResponse.Error);
    }

    [Fact]
    public async Task Token_WhenJwtServiceReturnsEmptyToken_ReturnsSuccessfully()
    {
        var request = ClientCredentialsRequestBuilder.Create().Build();
        var testClient = OAuthClientBuilder.Create().Build();

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read" });

        _mockJwtTokenService
            .Setup(s => s.GenerateClientAccessToken(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(string.Empty);

        _mockJwtTokenService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        var result = await _controller.Token(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
        Assert.Equal(string.Empty, tokenResponse.AccessToken);
    }

    [Theory]
    [InlineData("read write  admin")]
    [InlineData("read,write,admin")]
    [InlineData("read\twrite\nadmin")]
    [InlineData("READ WRITE")]
    public async Task Token_WithMalformedScopeStrings_HandlesGracefully(string scope)
    {
        var request = ClientCredentialsRequestBuilder.Create()
            .WithScope(scope)
            .Build();

        var testClient = OAuthClientBuilder.Create()
            .WithAllowedScopes("read,write,admin")
            .Build();

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read", "write" });

        _mockJwtTokenService
            .Setup(s => s.GenerateClientAccessToken(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns("test-token");

        _mockJwtTokenService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(3600);

        var result = await _controller.Token(request);

        Assert.IsType<OkObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(int.MaxValue)]
    public async Task Token_WithBoundaryExpirationTimes_HandlesCorrectly(int expirationSeconds)
    {
        var request = ClientCredentialsRequestBuilder.Create().Build();
        var testClient = OAuthClientBuilder.Create().Build();

        _mockOAuthClientService
            .Setup(s => s.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret))
            .ReturnsAsync(testClient);

        _mockOAuthClientService
            .Setup(s => s.DetermineGrantedScopes(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "read" });

        _mockJwtTokenService
            .Setup(s => s.GenerateClientAccessToken(testClient, It.IsAny<IEnumerable<string>>()))
            .Returns("test-token");

        _mockJwtTokenService
            .Setup(s => s.GetAccessTokenExpirationSeconds())
            .Returns(expirationSeconds);

        var result = await _controller.Token(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var tokenResponse = Assert.IsType<TokenResponse>(okResult.Value);
        Assert.Equal(expirationSeconds, tokenResponse.ExpiresIn);
    }
}
