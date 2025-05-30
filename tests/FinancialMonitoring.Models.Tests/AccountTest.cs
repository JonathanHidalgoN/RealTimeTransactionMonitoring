namespace FinancialMonitoring.Models.Tests;

public class AccountTests
{
    [Fact]
    public void Constructor_WithValidAccountId_ShouldCreateInstance()
    {
        string validAccountId = "ACC12345";
        var account = new Account(validAccountId);
        Assert.NotNull(account);
        Assert.Equal(validAccountId, account.AccountId);
    }

    //Run 3 times with the data invalidAccountId 
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
