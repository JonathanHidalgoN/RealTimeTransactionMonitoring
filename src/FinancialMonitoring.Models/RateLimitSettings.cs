using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// Configuration settings for IP-based rate limiting.
/// These settings control the general behavior of the rate limiting middleware.
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Enable endpoint-specific rate limiting rules.
    /// Default: true - Allows different limits per endpoint.
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;

    /// <summary>
    /// Whether to stack blocked requests in queue or reject immediately.
    /// Default: false - Reject blocked requests immediately for better performance.
    /// </summary>
    public bool StackBlockedRequests { get; set; } = false;

    /// <summary>
    /// HTTP status code to return when rate limit is exceeded.
    /// Default: 429 (Too Many Requests) - Standard rate limiting status code.
    /// </summary>
    [Range(400, 599)]
    public int HttpStatusCode { get; set; } = 429;

    /// <summary>
    /// Header name to read the real client IP address from.
    /// Default: "X-Real-IP" - Common header for reverse proxy setups.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string RealIpHeader { get; set; } = "X-Real-IP";

    /// <summary>
    /// Header name to read the client identifier from.
    /// Default: "X-ClientId" - Allows per-client rate limiting.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ClientIdHeader { get; set; } = "X-ClientId";

    /// <summary>
    /// Rate limiting rules for different endpoints.
    /// Each rule defines endpoint pattern, time period, and request limit.
    /// </summary>
    public List<RateLimitRuleSettings> GeneralRules { get; set; } = new();
}

/// <summary>
/// Configuration for individual rate limiting rules.
/// Defines limits for specific endpoint patterns.
/// </summary>
public class RateLimitRuleSettings
{
    /// <summary>
    /// Endpoint pattern to match. Use * for wildcards.
    /// Examples: "*", "*/transactions", "/api/v1/analytics"
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; set; } = "*";

    /// <summary>
    /// Time period for the rate limit.
    /// Format: "{number}{unit}" where unit is s, m, h, d
    /// Examples: "1m" (1 minute), "1h" (1 hour), "1d" (1 day)
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^\d+[smhd]$", ErrorMessage = "Period must be in format like '1m', '1h', '1d'")]
    public string Period { get; set; } = "1m";

    /// <summary>
    /// Maximum number of requests allowed in the specified period.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Limit { get; set; } = 1000;
}