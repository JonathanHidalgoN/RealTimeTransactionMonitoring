using TransactionSimulator;
using FinancialMonitoring.Models;

public class SimulatorTests
{
    //Makes method as a test case
    [Fact]
    public void GenerateTransaction_ShouldReturnValidTransaction_WhenCalled()
    {
        Transaction generatedTransaction = Simulator.GenerateTransaction();
        Assert.NotNull(generatedTransaction);

        // 2. Check id
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.Id));
        // 3. Check Amount
        Assert.True(generatedTransaction.Amount >= 0, "Amount should be non-negative.");
        Assert.True(generatedTransaction.Amount <= 1500, "Amount should be less than or equal to 1000 (based on generation logic).");
        // 4. Check Timestamp
        long currentUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.True(generatedTransaction.Timestamp <= currentUnixTimeMs, "Timestamp should not be in the future.");

        // 5. Check SourceAccount
        Assert.NotNull(generatedTransaction.SourceAccount);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.SourceAccount.AccountId));

        // 6. Check DestinationAccount
        Assert.NotNull(generatedTransaction.DestinationAccount);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.DestinationAccount.AccountId));
    }
}
