using System.Security.Cryptography;
using FinancialMonitoring.Abstractions;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// Service implementation for password hashing and verification operations using PBKDF2
/// </summary>
public class PasswordHashingService : IPasswordHashingService
{
    private readonly ILogger<PasswordHashingService> _logger;

    private const int SaltSize = 128 / 8; // 128 bits (16 bytes)
    private const int KeySize = 256 / 8; // 256 bits (32 bytes)
    private const int Iterations = 100000; // OWASP recommendation as of 2023
    private const char Delimiter = ';';

    public PasswordHashingService(ILogger<PasswordHashingService> logger)
    {
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var saltBytes = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256);

        var hashBytes = pbkdf2.GetBytes(KeySize);

        var result = $"{Iterations}{Delimiter}{Convert.ToBase64String(saltBytes)}{Delimiter}{Convert.ToBase64String(hashBytes)}";

        _logger.LogDebug("Password hashed successfully using PBKDF2-HMAC-SHA256 with {Iterations} iterations", Iterations);
        return result;
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));

        try
        {
            var parts = hashedPassword.Split(Delimiter);
            if (parts.Length != 3)
            {
                _logger.LogWarning("Invalid hash format - expected 3 parts separated by '{Delimiter}'", Delimiter);
                return false;
            }

            var iterations = int.Parse(parts[0]);
            var saltBytes = Convert.FromBase64String(parts[1]);
            var storedHashBytes = Convert.FromBase64String(parts[2]);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                iterations,
                HashAlgorithmName.SHA256);

            var computedHashBytes = pbkdf2.GetBytes(KeySize);

            var isValid = CryptographicOperations.FixedTimeEquals(computedHashBytes, storedHashBytes);

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
