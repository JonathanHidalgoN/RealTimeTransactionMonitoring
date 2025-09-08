namespace FinancialMonitoring.Models.Tests;

public class AccountTests
{
    /// <summary>
    /// This test creates an Account with a valid account ID and verifies the instance is created correctly
    /// </summary>
    [Fact]
    public void Constructor_WithValidAccountId_ShouldCreateInstance()
    {
        string validAccountId = "ACC12345";
        var account = new Account(validAccountId);
        Assert.NotNull(account);
        Assert.Equal(validAccountId, account.AccountId);
    }

    /// <summary>
    /// This test verifies that Account constructor throws ArgumentException when given null, empty, or whitespace account IDs
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidAccountId_ShouldThrowArgumentException(string invalidAccountId)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Account(invalidAccountId));
        Assert.Equal("accountId", exception.ParamName);
    }
}
