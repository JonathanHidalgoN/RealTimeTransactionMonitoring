using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models.Analytics;

/// <summary>
/// Represents global transaction analytics and statistics.
/// </summary>
public record TransactionAnalytics
{
    /// <summary>
    /// Total number of transactions in the system.
    /// </summary>
    [JsonPropertyName("totalTransactions")]
    public long TotalTransactions { get; init; }

    /// <summary>
    /// Total number of anomalous transactions.
    /// </summary>
    [JsonPropertyName("totalAnomalies")]
    public long TotalAnomalies { get; init; }

    /// <summary>
    /// Percentage of transactions flagged as anomalous.
    /// </summary>
    [JsonPropertyName("anomalyRate")]
    public double AnomalyRate { get; init; }

    /// <summary>
    /// Total transaction volume (sum of all transaction amounts).
    /// </summary>
    [JsonPropertyName("totalVolume")]
    public double TotalVolume { get; init; }

    /// <summary>
    /// Average transaction amount.
    /// </summary>
    [JsonPropertyName("averageAmount")]
    public double AverageAmount { get; init; }

    /// <summary>
    /// Number of unique accounts involved in transactions.
    /// </summary>
    [JsonPropertyName("uniqueAccounts")]
    public long UniqueAccounts { get; init; }

    /// <summary>
    /// Number of transactions in the last 24 hours.
    /// </summary>
    [JsonPropertyName("transactionsLast24Hours")]
    public long TransactionsLast24Hours { get; init; }

    /// <summary>
    /// Number of anomalies detected in the last 24 hours.
    /// </summary>
    [JsonPropertyName("anomaliesLast24Hours")]
    public long AnomaliesLast24Hours { get; init; }

    /// <summary>
    /// Timestamp when these analytics were calculated.
    /// </summary>
    [JsonPropertyName("calculatedAt")]
    public long CalculatedAt { get; init; }

    public TransactionAnalytics(
        long totalTransactions,
        long totalAnomalies,
        double totalVolume,
        double averageAmount,
        long uniqueAccounts,
        long transactionsLast24Hours,
        long anomaliesLast24Hours)
    {
        TotalTransactions = totalTransactions;
        TotalAnomalies = totalAnomalies;
        AnomalyRate = totalTransactions > 0 ? (double)totalAnomalies / totalTransactions : 0.0;
        TotalVolume = totalVolume;
        AverageAmount = averageAmount;
        UniqueAccounts = uniqueAccounts;
        TransactionsLast24Hours = transactionsLast24Hours;
        AnomaliesLast24Hours = anomaliesLast24Hours;
        CalculatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}