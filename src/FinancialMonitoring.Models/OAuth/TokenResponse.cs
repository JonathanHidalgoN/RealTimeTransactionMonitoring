using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models.OAuth;

/// <summary>
/// OAuth2 Token Response (RFC 6749 Section 5.1)
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// The access token issued by the authorization server
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// The type of token (always "Bearer" for JWT)
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Lifetime in seconds of the access token
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Refresh token (if applicable)
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Scope of the access token (space-delimited)
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// Creates a successful token response for client credentials flow
    /// </summary>
    public static TokenResponse ForClientCredentials(string accessToken, int expiresInSeconds, IEnumerable<string>? scopes = null)
    {
        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = expiresInSeconds,
            Scope = scopes != null ? string.Join(" ", scopes) : null
        };
    }

    /// <summary>
    /// Creates a successful token response with refresh token
    /// </summary>
    public static TokenResponse WithRefreshToken(string accessToken, string refreshToken, int expiresInSeconds, IEnumerable<string>? scopes = null)
    {
        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = expiresInSeconds,
            Scope = scopes != null ? string.Join(" ", scopes) : null
        };
    }
}