using FinancialMonitoring.Models;
using FinancialMonitoring.Models.OAuth;

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
    /// Generates a new access token for an OAuth client
    /// </summary>
    /// <param name="client">The OAuth client</param>
    /// <param name="scopes">The granted scopes</param>
    /// <returns>JWT access token</returns>
    string GenerateClientAccessToken(OAuthClient client, IEnumerable<string> scopes);

    /// <summary>
    /// Generates a new refresh token
    /// </summary>
    /// <param name="userId">Optional user ID to associate with the refresh token</param>
    string GenerateRefreshToken(int? userId = null);

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

    /// <summary>
    /// Gets the expiration duration in seconds for access tokens
    /// </summary>
    int GetAccessTokenExpirationSeconds();
}
