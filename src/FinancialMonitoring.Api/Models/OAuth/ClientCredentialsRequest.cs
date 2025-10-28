using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialMonitoring.Api.Models.OAuth;

/// <summary>
/// OAuth2 Client Credentials Grant request model (RFC 6749 Section 4.4)
/// </summary>
public class ClientCredentialsRequest
{
    /// <summary>
    /// OAuth2 grant type - must be "client_credentials"
    /// </summary>
    [JsonPropertyName("grant_type")]
    [FromForm(Name = "grant_type")]
    [Required]
    public string GrantType { get; set; } = "client_credentials";

    /// <summary>
    /// Client identifier
    /// </summary>
    [JsonPropertyName("client_id")]
    [FromForm(Name = "client_id")]
    [Required]
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret
    /// </summary>
    [JsonPropertyName("client_secret")]
    [FromForm(Name = "client_secret")]
    [Required]
    [MaxLength(255)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Requested scope (optional, space-delimited)
    /// </summary>
    [JsonPropertyName("scope")]
    [FromForm(Name = "scope")]
    [MaxLength(500)]
    public string? Scope { get; set; }

    /// <summary>
    /// Gets the requested scopes as a list
    /// </summary>
    public IEnumerable<string> GetRequestedScopes()
    {
        if (string.IsNullOrWhiteSpace(Scope))
            return Enumerable.Empty<string>();

        return Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => !string.IsNullOrEmpty(s));
    }
}
