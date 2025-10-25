namespace FinancialMonitoring.Abstractions;

/// <summary>
/// Service interface for password hashing and verification operations using PBKDF2
/// Salt is generated internally and embedded in the hash output
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes a password using PBKDF2-HMAC-SHA256 with an internally generated salt
    /// The salt is embedded in the returned hash string
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>Hash string containing salt and hash (format: iterations;salt;hash)</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a stored hash
    /// Extracts the salt from the hash string and re-hashes the password for comparison
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The stored password hash (containing embedded salt)</param>
    /// <returns>True if the password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword);
}
