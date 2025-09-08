using TransactionSimulator.Generation;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models;

public class SimulatorTests
{
    /// <summary>
    /// This test verifies that the transaction generator creates valid transactions with all required properties populated
    /// </summary>
    [Fact]
    public void GenerateTransaction_ShouldReturnValidTransaction_WhenCalled()
    {
        ITransactionGenerator generator = new TransactionGenerator(seed: 12345);
        Transaction generatedTransaction = generator.GenerateTransaction();

        Assert.NotNull(generatedTransaction);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.Id));
        Assert.True(generatedTransaction.Amount > 0);

        var currentTime = DateTimeOffset.UtcNow;
        var transactionTime = DateTimeOffset.FromUnixTimeMilliseconds(generatedTransaction.Timestamp);
        var timeDifferenceHours = Math.Abs((transactionTime - currentTime).TotalHours);

        Assert.True(timeDifferenceHours <= 24, $"Transaction timestamp should be within 24 hours of current time. Actual difference: {timeDifferenceHours:F2} hours");

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
