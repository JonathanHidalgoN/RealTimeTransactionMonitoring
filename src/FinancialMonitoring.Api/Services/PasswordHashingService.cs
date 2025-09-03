using System.Security.Cryptography;
using System.Text;
using FinancialMonitoring.Abstractions;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// Service implementation for password hashing and verification operations
/// </summary>
public class PasswordHashingService : IPasswordHashingService
{
    private readonly ILogger<PasswordHashingService> _logger;

    public PasswordHashingService(ILogger<PasswordHashingService> logger)
    {
        _logger = logger;
    }

    public string GenerateRandomSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        _logger.LogDebug("Generated new random salt");
        return salt;
    }

    public string HashPassword(string password, string salt)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        var hash = Convert.ToBase64String(hashedBytes);

        _logger.LogDebug("Password hashed successfully");
        return hash;
    }

    public bool VerifyPassword(string password, string hashedPassword, string salt)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));

        if (string.IsNullOrWhiteSpace(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        try
        {
            var hashedInput = HashPassword(password, salt);
            var isValid = hashedInput == hashedPassword;

            _logger.LogDebug("Password verification result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password verification");
            return false;
        }
    }
}
