using FinancialMonitoring.Models;
namespace FinancialMonitoring.Abstractions;

/// <summary>
/// Service interface for JWT token generation and validation
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a new access token for the user
    /// </summary>
    string GenerateAccessToken(AuthUser user);

    /// <summary>
    /// Generates a new refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a refresh token and returns the associated user ID
    /// </summary>
    int? ValidateRefreshToken(string refreshToken);

    /// <summary>
    /// Invalidates a refresh token (for logout)
    /// </summary>
    void InvalidateRefreshToken(string refreshToken);

    /// <summary>
    /// Gets the expiration time for access tokens
    /// </summary>
    DateTime GetAccessTokenExpiration();
}
