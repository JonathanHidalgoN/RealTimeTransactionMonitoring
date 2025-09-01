namespace FinancialMonitoring.Abstractions;

/// <summary>
/// Service interface for password hashing and verification operations
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    /// <returns>Base64-encoded random salt</returns>
    string GenerateRandomSalt();

    /// <summary>
    /// Hashes a password with the provided salt
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <param name="salt">The salt to use for hashing</param>
    /// <returns>Base64-encoded password hash</returns>
    string HashPassword(string password, string salt);

    /// <summary>
    /// Verifies a password against a stored hash and salt
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The stored password hash</param>
    /// <param name="salt">The salt used for the stored hash</param>
    /// <returns>True if the password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword, string salt);
}