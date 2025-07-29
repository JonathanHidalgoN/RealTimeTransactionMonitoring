using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions;

namespace TransactionSimulator.Generation;

/// <summary>
/// Simple transaction generator for testing or basic scenarios
/// </summary>
public class SimpleTransactionGenerator : ITransactionGenerator
{
    private readonly Random _random;

    public SimpleTransactionGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Generates a simple transaction for testing purposes
    /// </summary>
    public Transaction GenerateTransaction()
    {
        return new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: Math.Round(_random.NextDouble() * 1000, 2),
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sourceAccount: new Account($"ACC{_random.Next(1000, 9999)}"),
            destinationAccount: new Account($"ACC{_random.Next(1000, 9999)}"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: "Simple Test Store",
            location: new Location("Test City", "TC", "US")
        );
    }
}
