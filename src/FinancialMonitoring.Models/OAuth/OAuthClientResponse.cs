namespace FinancialMonitoring.Models.OAuth;

/// <summary>
/// Response model for OAuth client information
/// </summary>
public class OAuthClientResponse
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AllowedScopes { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Request model for creating OAuth clients
/// </summary>
public class CreateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IEnumerable<string>? AllowedScopes { get; set; }
}
