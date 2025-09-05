using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models.OAuth;

/// <summary>
/// OAuth2 Error Response (RFC 6749 Section 5.2)
/// </summary>
public class OAuthErrorResponse
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error description
    /// </summary>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// URI with error information
    /// </summary>
    [JsonPropertyName("error_uri")]
    public string? ErrorUri { get; set; }

    /// <summary>
    /// Creates an invalid_client error
    /// </summary>
    public static OAuthErrorResponse InvalidClient(string? description = null)
    {
        return new OAuthErrorResponse
        {
            Error = "invalid_client",
            ErrorDescription = description ?? "Client authentication failed"
        };
    }

    /// <summary>
    /// Creates an invalid_grant error
    /// </summary>
    public static OAuthErrorResponse InvalidGrant(string? description = null)
    {
        return new OAuthErrorResponse
        {
            Error = "invalid_grant",
            ErrorDescription = description ?? "The provided authorization grant is invalid"
        };
    }

    /// <summary>
    /// Creates an invalid_scope error
    /// </summary>
    public static OAuthErrorResponse InvalidScope(string? description = null)
    {
        return new OAuthErrorResponse
        {
            Error = "invalid_scope",
            ErrorDescription = description ?? "The requested scope is invalid"
        };
    }

    /// <summary>
    /// Creates an unsupported_grant_type error
    /// </summary>
    public static OAuthErrorResponse UnsupportedGrantType(string? description = null)
    {
        return new OAuthErrorResponse
        {
            Error = "unsupported_grant_type",
            ErrorDescription = description ?? "The grant type is not supported"
        };
    }

    /// <summary>
    /// Creates an invalid_request error
    /// </summary>
    public static OAuthErrorResponse InvalidRequest(string? description = null)
    {
        return new OAuthErrorResponse
        {
            Error = "invalid_request",
            ErrorDescription = description ?? "The request is missing a required parameter or is otherwise malformed"
        };
    }
}