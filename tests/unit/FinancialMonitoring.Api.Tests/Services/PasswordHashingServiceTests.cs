using FinancialMonitoring.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancialMonitoring.Api.Tests.Services;

public class PasswordHashingServiceTests
{
    private readonly PasswordHashingService _service;

    public PasswordHashingServiceTests()
    {
        var mockLogger = new Mock<ILogger<PasswordHashingService>>();
        _service = new PasswordHashingService(mockLogger.Object);
    }

    [Fact]
    public void HashPassword_WithPassword_ReturnsHashedPassword()
    {
        var password = "TestPassword123!";

        var hash = _service.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_WithSamePassword_ReturnsDifferentHashes()
    {
        var password = "TestPassword123!";

        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_ContainsSaltInformation()
    {
        var password = "TestPassword123!";

        var hash = _service.HashPassword(password);

        var parts = hash.Split(';');
        Assert.Equal(3, parts.Length);
        Assert.True(int.TryParse(parts[0], out var iterations));
        Assert.True(iterations > 0);

        var saltBytes = Convert.FromBase64String(parts[1]);
        Assert.NotEmpty(saltBytes);

        var hashBytes = Convert.FromBase64String(parts[2]);
        Assert.NotEmpty(hashBytes);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "TestPassword123!";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(wrongPassword, hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ReturnsFalse()
    {
        var password = "TestPassword123!";
        var invalidHash = "invalid-hash-format";

        var result = _service.VerifyPassword(password, invalidHash);

        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithInvalidPassword_ThrowsArgumentException(string? invalidPassword)
    {
        Assert.Throws<ArgumentException>(() => _service.HashPassword(invalidPassword!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_WithInvalidPassword_ThrowsArgumentException(string? invalidPassword)
    {
        var hash = "dummy-hash";

        Assert.Throws<ArgumentException>(() => _service.VerifyPassword(invalidPassword!, hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_WithInvalidHash_ThrowsArgumentException(string? invalidHash)
    {
        var password = "TestPassword123!";

        Assert.Throws<ArgumentException>(() => _service.VerifyPassword(password, invalidHash!));
    }

    [Fact]
    public void HashPassword_GeneratesConsistentFormatHashes()
    {
        var passwords = new[] { "short", "a-much-longer-password-with-special-chars-123!", "pwd" };

        var hashes = passwords.Select(p => _service.HashPassword(p)).ToArray();

        Assert.All(hashes, hash =>
        {
            var parts = hash.Split(';');
            Assert.Equal(3, parts.Length);
        });
    }

    [Fact]
    public void PasswordHashingService_SupportsVariousPasswordComplexities()
    {
        var testCases = new[]
        {
            "simple",
            "Complex123!",
            "àéîôùç",
            "😀🔒💻",
            "Pass with spaces and 123!",
            new string('a', 100)
        };

        foreach (var password in testCases)
        {
            var hash = _service.HashPassword(password);
            var isValid = _service.VerifyPassword(password, hash);

            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.True(isValid, $"Failed for password: {password}");
        }
    }
}
