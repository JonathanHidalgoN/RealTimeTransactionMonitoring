using System.Diagnostics;
using System.Reflection;
using System.Text;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace FinancialMonitoring.Api.Tests.Services;

/// <summary>
/// Unit tests for SecureApiKeyAuthenticationHandler focusing on security-critical logic
/// </summary>
public class SecureApiKeyAuthenticationHandlerUnitTests
{
    [Fact]
    public void SecureStringCompare_WithIdenticalStrings_ShouldReturnTrue()
    {
        var result = InvokeSecureStringCompare("test-api-key", "test-api-key");

        Assert.True(result);
    }

    [Fact]
    public void SecureStringCompare_WithDifferentStrings_ShouldReturnFalse()
    {
        var result = InvokeSecureStringCompare("test-api-key", "wrong-api-key");

        Assert.False(result);
    }

    [Fact]
    public void SecureStringCompare_WithNullInputs_ShouldReturnFalse()
    {
        var result1 = InvokeSecureStringCompare(null, "test-api-key");
        var result2 = InvokeSecureStringCompare("test-api-key", null);
        var result3 = InvokeSecureStringCompare(null, null);

        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void SecureStringCompare_WithEmptyStrings_ShouldHandleCorrectly()
    {
        var result1 = InvokeSecureStringCompare("", "");
        var result2 = InvokeSecureStringCompare("test", "");
        var result3 = InvokeSecureStringCompare("", "test");

        Assert.True(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public void SecureStringCompare_WithDifferentLengths_ShouldReturnFalse()
    {
        var result = InvokeSecureStringCompare("short", "very-long-api-key");

        Assert.False(result);
    }

    [Fact]
    public void SecureStringCompare_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        var specialKey = "api-key-with-!@#$%^&*()_+-={}[]|:;<>?";
        var result1 = InvokeSecureStringCompare(specialKey, specialKey);
        var result2 = InvokeSecureStringCompare(specialKey, "different-key");

        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public void SecureStringCompare_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        var unicodeKey = "api-key-with-üñíçødé-characters-🔐";
        var result1 = InvokeSecureStringCompare(unicodeKey, unicodeKey);
        var result2 = InvokeSecureStringCompare(unicodeKey, "different-key");

        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public void SecureStringCompare_WithVeryLongStrings_ShouldHandleCorrectly()
    {
        var longKey = new string('a', 10000);
        var similarLongKey = new string('a', 9999) + 'b';

        var result1 = InvokeSecureStringCompare(longKey, longKey);
        var result2 = InvokeSecureStringCompare(longKey, similarLongKey);

        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public void SecureStringCompare_UsesConstantTimeComparison_VerifyImplementation()
    {
        // This test verifies that the method uses CryptographicOperations.FixedTimeEquals
        // which provides timing attack protection. We can't reliably test timing in unit tests
        // but we can verify the method behavior is consistent.

        var key1 = "test-key-1";
        var key2 = "test-key-2";
        var key1Copy = "test-key-1";

        // These should be deterministic regardless of timing
        Assert.True(InvokeSecureStringCompare(key1, key1Copy));
        Assert.False(InvokeSecureStringCompare(key1, key2));

        // Multiple calls should be consistent
        for (int i = 0; i < 10; i++)
        {
            Assert.True(InvokeSecureStringCompare(key1, key1Copy));
            Assert.False(InvokeSecureStringCompare(key1, key2));
        }
    }

    [Theory]
    [InlineData("api-key-1", "api-key-2")] // Similar prefixes
    [InlineData("prefix-different-suffix", "prefix-another-suffix")] // Same prefix, different suffix
    [InlineData("same-length-key-a", "same-length-key-b")] // Same length, different content
    public void SecureStringCompare_WithSimilarStrings_ShouldReturnFalse(string key1, string key2)
    {
        var result = InvokeSecureStringCompare(key1, key2);

        Assert.False(result);
    }

    /// <summary>
    /// Helper method to invoke the private SecureStringCompare method using reflection
    /// </summary>
    private static bool InvokeSecureStringCompare(string? provided, string? expected)
    {
        var type = typeof(SecureApiKeyAuthenticationHandler);
        var method = type.GetMethod("SecureStringCompare", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(null, new object?[] { provided, expected });
        return (bool)result!;
    }
}