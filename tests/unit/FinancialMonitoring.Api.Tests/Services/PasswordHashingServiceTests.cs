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
    public void GenerateRandomSalt_ReturnsNonEmptyString()
    {

        var salt = _service.GenerateRandomSalt();


        Assert.NotNull(salt);
        Assert.NotEmpty(salt);
    }

    [Fact]
    public void GenerateRandomSalt_ReturnsDifferentSaltsEachTime()
    {

        var salt1 = _service.GenerateRandomSalt();
        var salt2 = _service.GenerateRandomSalt();
        var salt3 = _service.GenerateRandomSalt();


        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(salt2, salt3);
        Assert.NotEqual(salt1, salt3);
    }

    [Fact]
    public void GenerateRandomSalt_ReturnsBase64String()
    {

        var salt = _service.GenerateRandomSalt();


        var bytes = Convert.FromBase64String(salt);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void HashPassword_WithPasswordAndSalt_ReturnsHashedPassword()
    {

        var password = "TestPassword123!";
        var salt = _service.GenerateRandomSalt();


        var hash = _service.HashPassword(password, salt);


        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_WithSamePasswordAndSalt_ReturnsSameHash()
    {

        var password = "TestPassword123!";
        var salt = _service.GenerateRandomSalt();


        var hash1 = _service.HashPassword(password, salt);
        var hash2 = _service.HashPassword(password, salt);


        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_WithSamePasswordDifferentSalt_ReturnsDifferentHashes()
    {

        var password = "TestPassword123!";
        var salt1 = _service.GenerateRandomSalt();
        var salt2 = _service.GenerateRandomSalt();


        var hash1 = _service.HashPassword(password, salt1);
        var hash2 = _service.HashPassword(password, salt2);


        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {

        var password = "TestPassword123!";
        var salt = _service.GenerateRandomSalt();
        var hash = _service.HashPassword(password, salt);


        var result = _service.VerifyPassword(password, hash, salt);


        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {

        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var salt = _service.GenerateRandomSalt();
        var hash = _service.HashPassword(password, salt);


        var result = _service.VerifyPassword(wrongPassword, hash, salt);


        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectSalt_ReturnsFalse()
    {

        var password = "TestPassword123!";
        var salt1 = _service.GenerateRandomSalt();
        var salt2 = _service.GenerateRandomSalt();
        var hash = _service.HashPassword(password, salt1);


        var result = _service.VerifyPassword(password, hash, salt2);


        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithInvalidPassword_ThrowsArgumentException(string? invalidPassword)
    {

        var salt = _service.GenerateRandomSalt();

        Assert.Throws<ArgumentException>(() => _service.HashPassword(invalidPassword!, salt));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithInvalidSalt_ThrowsArgumentException(string? invalidSalt)
    {

        var password = "TestPassword123!";

        Assert.Throws<ArgumentException>(() => _service.HashPassword(password, invalidSalt!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void VerifyPassword_WithInvalidPassword_ThrowsArgumentException(string? invalidPassword)
    {

        var salt = _service.GenerateRandomSalt();
        var hash = "dummy-hash";

        Assert.Throws<ArgumentException>(() => _service.VerifyPassword(invalidPassword!, hash, salt));
    }

    [Fact]
    public void HashPassword_GeneratesConsistentLengthHashes()
    {

        var passwords = new[] { "short", "a-much-longer-password-with-special-chars-123!", "pwd" };
        var salt = _service.GenerateRandomSalt();


        var hashes = passwords.Select(p => _service.HashPassword(p, salt)).ToArray();


        var firstHashLength = hashes[0].Length;
        Assert.All(hashes, hash => Assert.Equal(firstHashLength, hash.Length));
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

        var salt = _service.GenerateRandomSalt();

        foreach (var password in testCases)
        {

            var hash = _service.HashPassword(password, salt);
            var isValid = _service.VerifyPassword(password, hash, salt);


            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.True(isValid, $"Failed for password: {password}");
        }
    }
}
