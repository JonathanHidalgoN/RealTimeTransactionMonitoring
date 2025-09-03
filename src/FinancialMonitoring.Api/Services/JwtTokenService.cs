using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FinancialMonitoring.Abstractions;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// JWT token service implementation
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    //TODO: USE DATABASE
    private static readonly Dictionary<string, RefreshTokenData> _refreshTokens = new();

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateAccessToken(AuthUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("firstName", user.FirstName ?? ""),
            new("lastName", user.LastName ?? "")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("Generated access token for user {Username}", user.Username);
        return tokenString;
    }

    public string GenerateRefreshToken()
    {
        return GenerateRefreshToken(null);
    }

    public string GenerateRefreshToken(int? userId)
    {
        var refreshToken = Guid.NewGuid().ToString();

        _refreshTokens[refreshToken] = new RefreshTokenData
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            UserId = userId
        };

        _logger.LogInformation("Generated refresh token for user {UserId}", userId);
        return refreshToken;
    }

    public void StoreRefreshToken(string refreshToken, int userId)
    {
        _refreshTokens[refreshToken] = new RefreshTokenData
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            UserId = userId
        };

        _logger.LogInformation("Stored refresh token for user {UserId}", userId);
    }

    public int? ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Null or empty refresh token provided");
            return null;
        }

        if (!_refreshTokens.TryGetValue(refreshToken, out var tokenData))
        {
            _logger.LogWarning("Invalid refresh token provided");
            return null;
        }

        if (tokenData.ExpiresAt <= DateTime.UtcNow)
        {
            _refreshTokens.Remove(refreshToken);
            _logger.LogWarning("Expired refresh token provided");
            return null;
        }

        return tokenData.UserId;
    }

    public void InvalidateRefreshToken(string refreshToken)
    {
        if (_refreshTokens.Remove(refreshToken))
        {
            _logger.LogInformation("Refresh token invalidated");
        }
        else
        {
            _logger.LogWarning("Attempted to invalidate non-existent refresh token");
        }
    }

    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);
    }
}

/// <summary>
/// Data structure for storing refresh token information
/// </summary>
internal class RefreshTokenData
{
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? UserId { get; set; }
}
