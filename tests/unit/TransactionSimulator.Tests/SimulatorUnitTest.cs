using TransactionSimulator.Generation;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models;

public class SimulatorTests
{
    [Fact]
    public void GenerateTransaction_ShouldReturnValidTransaction_WhenCalled()
    {
        ITransactionGenerator generator = new TransactionGenerator();
        Transaction transaction = generator.GenerateTransaction();

        Assert.NotNull(transaction);
        Assert.False(string.IsNullOrWhiteSpace(transaction.Id));
        Assert.True(transaction.Amount > 0);
        Assert.NotNull(transaction.SourceAccount);
        Assert.StartsWith("ACC", transaction.SourceAccount.AccountId);
        Assert.NotNull(transaction.DestinationAccount);
        Assert.False(string.IsNullOrWhiteSpace(transaction.MerchantName));
        Assert.NotNull(transaction.Location);
        Assert.Equal("USD", transaction.Currency);
    }

    [Fact]
    public void GenerateTransaction_ShouldProduceDifferentResults_WhenCalledMultipleTimes()
    {
        var generator = new TransactionGenerator();

        var transaction1 = generator.GenerateTransaction();
        var transaction2 = generator.GenerateTransaction();

        Assert.True(transaction1.Amount != transaction2.Amount ||
                   transaction1.MerchantName != transaction2.MerchantName ||
                   transaction1.SourceAccount.AccountId != transaction2.SourceAccount.AccountId);
    }

    [Fact]
    public void GenerateTransaction_ShouldHaveReasonableAmounts_ForAllCategories()
    {
        var generator = new TransactionGenerator();
        var transactions = Enumerable.Range(0, 50)
            .Select(_ => generator.GenerateTransaction())
            .ToList();

        Assert.True(transactions.All(t => t.Amount > 0));
        Assert.True(transactions.All(t => t.Amount <= 10000));
    }
}
