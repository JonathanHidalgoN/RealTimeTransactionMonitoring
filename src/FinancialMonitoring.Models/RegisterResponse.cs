namespace FinancialMonitoring.Models;

/// <summary>
/// Response model for user registration
/// </summary>
public class RegisterResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AuthUserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
