using TransactionSimulator;
using TransactionSimulator.Generation;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models;

public class SimulatorTests
{
    [Fact]
    public void GenerateTransaction_ShouldReturnValidTransaction_WhenCalled()
    {
        ITransactionGenerator generator = new TransactionGenerator(seed: 12345);
        Transaction generatedTransaction = generator.GenerateTransaction();

        Assert.NotNull(generatedTransaction);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.Id));
        Assert.True(generatedTransaction.Amount > 0);

        long currentUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.True(generatedTransaction.Timestamp <= currentUnixTimeMs, "Timestamp should not be in the future.");

        Assert.NotNull(generatedTransaction.SourceAccount);
        Assert.StartsWith("ACC", generatedTransaction.SourceAccount.AccountId);

        Assert.NotNull(generatedTransaction.DestinationAccount);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.DestinationAccount.AccountId));

        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.MerchantName));
        Assert.NotNull(generatedTransaction.Location);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.Location.City));
        Assert.Equal("USD", generatedTransaction.Currency);
    }
}
