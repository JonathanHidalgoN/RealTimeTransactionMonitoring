using TransactionSimulator.Generation;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models;

public class SimulatorTests
{
    [Fact]
    public void GenerateTransaction_ShouldReturnValidTransaction_WhenCalled()
    {
        ITransactionGenerator generator = new TransactionGenerator(seed: 12345);
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
    public void GenerateTransaction_ShouldBeDeterministic_WithSameSeed()
    {
        var generator1 = new TransactionGenerator(seed: 12345);
        var generator2 = new TransactionGenerator(seed: 12345);

        var transaction1 = generator1.GenerateTransaction();
        var transaction2 = generator2.GenerateTransaction();

        Assert.Equal(transaction1.Amount, transaction2.Amount);
        Assert.Equal(transaction1.SourceAccount.AccountId, transaction2.SourceAccount.AccountId);
        Assert.Equal(transaction1.MerchantName, transaction2.MerchantName);
    }

    [Fact]
    public void GenerateTransaction_ShouldProduceDifferentResults_WithDifferentSeeds()
    {
        var generator1 = new TransactionGenerator(seed: 12345);
        var generator2 = new TransactionGenerator(seed: 54321);

        var transaction1 = generator1.GenerateTransaction();
        var transaction2 = generator2.GenerateTransaction();

        Assert.True(transaction1.Amount != transaction2.Amount || 
                   transaction1.MerchantName != transaction2.MerchantName);
    }

    [Fact]
    public void GenerateTransaction_ShouldHaveReasonableAmounts_ForAllCategories()
    {
        var generator = new TransactionGenerator(seed: 12345);
        var transactions = Enumerable.Range(0, 50)
            .Select(_ => generator.GenerateTransaction())
            .ToList();

        Assert.True(transactions.All(t => t.Amount > 0));
        Assert.True(transactions.All(t => t.Amount <= 10000));
    }
}
