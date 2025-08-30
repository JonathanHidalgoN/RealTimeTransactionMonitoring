using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// </summary>
public class JwtSettings
{

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required] public string SecretKey { get; set; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    [Range(1, 30)]
    public int RefreshTokenExpiryDays { get; set; } = 7;

    [Range(1, 30)]
    public int ClockSkewMinutes { get; set; } = 5;
    public bool RequireHttps { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
