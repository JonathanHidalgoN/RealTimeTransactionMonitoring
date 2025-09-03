using System.IdentityModel.Tokens.Jwt;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace FinancialMonitoring.Api.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IOptions<JwtSettings>> _mockOptions;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkewMinutes = 5
        };

        _mockOptions = new Mock<IOptions<JwtSettings>>();
        _mockOptions.Setup(x => x.Value).Returns(_jwtSettings);
        var mockLogger = new Mock<ILogger<JwtTokenService>>();
        _service = new JwtTokenService(_mockOptions.Object, mockLogger.Object);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ReturnsValidJwtToken()
    {
        var user = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Admin,
            FirstName = "Test",
            LastName = "User"
        };

        var token = _service.GenerateAccessToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        Assert.Equal(_jwtSettings.Issuer, jsonToken.Issuer);
        Assert.Equal(_jwtSettings.Audience, jsonToken.Audiences.First());
        Assert.Contains(jsonToken.Claims, c => c.Type == "nameid" && c.Value == "1");
        Assert.Contains(jsonToken.Claims, c => c.Type == "unique_name" && c.Value == "testuser");
        Assert.Contains(jsonToken.Claims, c => c.Type == "email" && c.Value == "test@example.com");
        Assert.Contains(jsonToken.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsValidGuid()
    {
        var refreshToken = _service.GenerateRefreshToken();

        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        Assert.True(Guid.TryParse(refreshToken, out _));
    }

    [Fact]
    public void GetAccessTokenExpiration_ReturnsCorrectExpirationTime()
    {
        var expiration = _service.GetAccessTokenExpiration();
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);

        Assert.True(Math.Abs((expiration - expectedExpiration).TotalSeconds) < 5);
    }

    [Fact]
    public void ValidateRefreshToken_WithValidToken_ReturnsUserId()
    {
        var refreshToken = Guid.NewGuid().ToString();
        var userId = 123;

        _service.StoreRefreshToken(refreshToken, userId);

        var result = _service.ValidateRefreshToken(refreshToken);

        Assert.Equal(userId, result);
    }

    [Fact]
    public void ValidateRefreshToken_WithInvalidToken_ReturnsNull()
    {
        var invalidToken = Guid.NewGuid().ToString();

        var result = _service.ValidateRefreshToken(invalidToken);

        Assert.Null(result);
    }

    [Fact]
    public void InvalidateRefreshToken_WithValidToken_RemovesToken()
    {
        var refreshToken = Guid.NewGuid().ToString();
        var userId = 123;

        _service.StoreRefreshToken(refreshToken, userId);
        Assert.Equal(userId, _service.ValidateRefreshToken(refreshToken));

        _service.InvalidateRefreshToken(refreshToken);

        Assert.Null(_service.ValidateRefreshToken(refreshToken));
    }

    [Fact]
    public void GenerateAccessToken_WithDifferentRoles_GeneratesCorrectClaims()
    {
        var roles = new[] { AuthUserRole.Admin, AuthUserRole.Analyst, AuthUserRole.Viewer };
        var handler = new JwtSecurityTokenHandler();

        foreach (var role in roles)
        {
            var user = new AuthUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = role,
                FirstName = "Test",
                LastName = "User"
            };

            var token = _service.GenerateAccessToken(user);
            var jsonToken = handler.ReadJwtToken(token);

            Assert.Contains(jsonToken.Claims, c => c.Type == "role" && c.Value == role.ToString());
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-token")]
    [InlineData("not.a.jwt.token")]
    public void ValidateRefreshToken_WithInvalidTokenFormats_ReturnsNull(string? token)
    {
        var result = _service.ValidateRefreshToken(token!);

        Assert.Null(result);
    }

    [Fact]
    public void GenerateAccessToken_TokenHasCorrectExpiration()
    {
        var user = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Admin
        };

        var token = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);
        var actualExpiry = jsonToken.ValidTo;

        Assert.True(Math.Abs((actualExpiry - expectedExpiry).TotalSeconds) < 10);
    }
}
