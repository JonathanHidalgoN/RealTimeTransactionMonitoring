using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models.OAuth;

/// <summary>
/// Represents an OAuth2 client for service-to-service authentication
/// </summary>
public class OAuthClient
{
    /// <summary>
    /// Unique identifier for the client
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Public client identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for authentication
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable client name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Client description
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of allowed scopes
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string AllowedScopes { get; set; } = string.Empty;

    /// <summary>
    /// Whether the client is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the client was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the client was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the client last authenticated (for monitoring)
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets the allowed scopes as a list
    /// </summary>
    public IEnumerable<string> GetAllowedScopes()
    {
        return AllowedScopes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s));
    }

    /// <summary>
    /// Checks if the client has a specific scope
    /// </summary>
    public bool HasScope(string scope)
    {
        return GetAllowedScopes().Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}