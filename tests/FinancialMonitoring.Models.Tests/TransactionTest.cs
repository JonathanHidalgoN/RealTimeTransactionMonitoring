
namespace FinancialMonitoring.Models.Tests;

public class TransactionTests
{
    private Account ValidSourceAccount() => new Account("ACC_SRC_VALID");
    private Account ValidDestinationAccount() => new Account("ACC_DEST_VALID");
    private Location ValidLocation() => new Location("New York", "NY", "US", "10001");
    
    private Transaction CreateValidTransaction(
        string? id = null, 
        double? amount = null, 
        long? timestamp = null,
        Account? sourceAccount = null,
        Account? destinationAccount = null,
        string? anomalyFlag = null)
    {
        return new Transaction(
            id ?? Guid.NewGuid().ToString(),
            amount ?? 100.50,
            timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sourceAccount ?? ValidSourceAccount(),
            destinationAccount ?? ValidDestinationAccount(),
            TransactionType.Purchase,
            MerchantCategory.Retail,
            "Test Merchant",
            ValidLocation(),
            "USD",
            PaymentMethod.DebitCard,
            anomalyFlag
        );
    }
    
    private Transaction CreateTransactionWithNullChecking(
        string id, 
        double amount, 
        long timestamp,
        Account sourceAccount,
        Account destinationAccount,
        string? anomalyFlag = null)
    {
        return new Transaction(
            id,
            amount,
            timestamp,
            sourceAccount,
            destinationAccount,
            TransactionType.Purchase,
            MerchantCategory.Retail,
            "Test Merchant",
            ValidLocation(),
            "USD",
            PaymentMethod.DebitCard,
            anomalyFlag
        );
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        string validId = Guid.NewGuid().ToString();
        double validAmount = 100.50;
        long validTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Account sourceAccount = ValidSourceAccount();
        Account destinationAccount = ValidDestinationAccount();

        var transaction = CreateValidTransaction(validId, validAmount, validTimestamp, sourceAccount, destinationAccount);

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
            CreateTransactionWithNullChecking(invalidId, 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), ValidDestinationAccount()));
        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateTransactionWithNullChecking(Guid.NewGuid().ToString(), -50.0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), ValidDestinationAccount()));
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
            CreateTransactionWithNullChecking(Guid.NewGuid().ToString(), 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), nullSourceAccount, ValidDestinationAccount()));
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
            CreateTransactionWithNullChecking(Guid.NewGuid().ToString(), 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ValidSourceAccount(), nullDestinationAccount));
#pragma warning restore CS8604
        Assert.Equal("destinationAccount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ShouldBeAllowed()
    {
        string validId = Guid.NewGuid().ToString();
        var transaction = CreateValidTransaction(validId, 0.0);
        Assert.NotNull(transaction);
        Assert.Equal(0.0, transaction.Amount);
    }
}
