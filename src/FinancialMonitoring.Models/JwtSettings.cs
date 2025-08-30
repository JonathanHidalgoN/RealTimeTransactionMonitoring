using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// Configuration settings for JWT authentication and token management
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The issuer of the JWT tokens (typically the application name or URL)
    /// </summary>
    [Required] public string Issuer { get; set; } = string.Empty;
    
    /// <summary>
    /// The intended audience for the JWT tokens (typically the client application)
    /// </summary>
    [Required] public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// The secret key used to sign and validate JWT tokens
    /// </summary>
    [Required] public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes (1-120 minutes)
    /// </summary>
    [Range(1, 120)]
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration time in days (1-30 days)
    /// </summary>
    [Range(1, 30)]
    public int RefreshTokenExpiryDays { get; set; } = 7;

    /// <summary>
    /// Clock skew tolerance in minutes to account for time differences between servers
    /// </summary>
    [Range(1, 30)]
    public int ClockSkewMinutes { get; set; } = 5;
    
    /// <summary>
    /// Whether to require HTTPS for token validation
    /// </summary>
    public bool RequireHttps { get; set; } = true;
    
    /// <summary>
    /// Whether to validate token lifetime (expiration)
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
    
    /// <summary>
    /// Whether to validate the token issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;
    
    /// <summary>
    /// Whether to validate the token audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
    
    /// <summary>
    /// Whether to validate the issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
