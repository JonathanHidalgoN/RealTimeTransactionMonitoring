using TransactionSimulator;
using FinancialMonitoring.Models;

public class SimulatorTests
{
    [Fact]
    public void GenerateTransaction_ShouldReturnValidTransaction_WhenCalled()
    {
        Transaction generatedTransaction = Simulator.GenerateTransaction();

        Assert.NotNull(generatedTransaction);
        Assert.False(string.IsNullOrWhiteSpace(generatedTransaction.Id));
        Assert.InRange(generatedTransaction.Amount, 0, 1500);

        long currentUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.True(generatedTransaction.Timestamp <= currentUnixTimeMs, "Timestamp should not be in the future.");

        Assert.NotNull(generatedTransaction.SourceAccount);
        Assert.StartsWith("ACC", generatedTransaction.SourceAccount.AccountId);

        Assert.NotNull(generatedTransaction.DestinationAccount);
        Assert.StartsWith("ACC", generatedTransaction.DestinationAccount.AccountId);
    }
}
