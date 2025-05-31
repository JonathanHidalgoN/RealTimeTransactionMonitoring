
namespace FinancialMonitoring.Models.Tests;

public class TransactionTests
{
    private Account ValidSourceAccount() => new Account("ACC_SRC_VALID");
    private Account ValidDestinationAccount() => new Account("ACC_DEST_VALID");

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        string validId = Guid.NewGuid().ToString();
        double validAmount = 100.50;
        long validTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Account sourceAccount = ValidSourceAccount();
        Account destinationAccount = ValidDestinationAccount();

        var transaction = new Transaction(validId, validAmount, validTimestamp, sourceAccount, destinationAccount);

        Assert.NotNull(transaction);
        Assert.Equal(validId, transaction.Id);
        Assert.Equal(validAmount, transaction.Amount);
        Assert.Equal(validTimestamp, transaction.Timestamp);
        Assert.Equal(sourceAccount, transaction.SourceAccount);
        Assert.Equal(destinationAccount, transaction.DestinationAccount);
        Assert.Null(transaction.AnomalyFlag);
    }

    //Run 3 times with the data invalid id
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidId_ShouldThrowArgumentException(string invalidId)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(invalidId, 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), ValidDestinationAccount()));
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Transaction(Guid.NewGuid().ToString(), -50.0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), ValidDestinationAccount()));
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSourceAccount_ShouldThrowArgumentNullException()
    {
#pragma warning disable CS8600
        Account nullSourceAccount = null;
#pragma warning restore CS8600

        var exception = Assert.Throws<ArgumentNullException>(() =>
#pragma warning disable CS8604
            new Transaction(Guid.NewGuid().ToString(), 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), nullSourceAccount, ValidDestinationAccount()));
#pragma warning restore CS8604
        Assert.Equal("sourceAccount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDestinationAccount_ShouldThrowArgumentNullException()
    {
#pragma warning disable CS8600
        Account nullDestinationAccount = null;
#pragma warning restore CS8600

        var exception = Assert.Throws<ArgumentNullException>(() =>
#pragma warning disable CS8604
            new Transaction(Guid.NewGuid().ToString(), 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), nullDestinationAccount));
#pragma warning restore CS8604
        Assert.Equal("destinationAccount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ShouldBeAllowed()
    {
        string validId = Guid.NewGuid().ToString();
        var transaction = new Transaction(validId, 0.0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), ValidDestinationAccount());
        Assert.NotNull(transaction);
        Assert.Equal(0.0, transaction.Amount);
    }
}
