using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models.Analytics;

/// <summary>
/// Represents analytics data for merchant transactions.
/// </summary>
public record MerchantAnalytics
{
    /// <summary>
    /// Name of the merchant.
    /// </summary>
    [JsonPropertyName("merchantName")]
    public string MerchantName { get; init; }

    /// <summary>
    /// Merchant category.
    /// </summary>
    [JsonPropertyName("category")]
    public MerchantCategory Category { get; init; }

    /// <summary>
    /// Total number of transactions for this merchant.
    /// </summary>
    [JsonPropertyName("transactionCount")]
    public long TransactionCount { get; init; }

    /// <summary>
    /// Total volume (sum of transaction amounts) for this merchant.
    /// </summary>
    [JsonPropertyName("totalVolume")]
    public double TotalVolume { get; init; }

    /// <summary>
    /// Average transaction amount for this merchant.
    /// </summary>
    [JsonPropertyName("averageAmount")]
    public double AverageAmount { get; init; }

    /// <summary>
    /// Number of anomalous transactions for this merchant.
    /// </summary>
    [JsonPropertyName("anomalyCount")]
    public long AnomalyCount { get; init; }

    /// <summary>
    /// Percentage of this merchant's transactions that are anomalous.
    /// </summary>
    [JsonPropertyName("anomalyRate")]
    public double AnomalyRate { get; init; }

    public MerchantAnalytics(
        string merchantName,
        MerchantCategory category,
        long transactionCount,
        double totalVolume,
        double averageAmount,
        long anomalyCount)
    {
        MerchantName = merchantName;
        Category = category;
        TransactionCount = transactionCount;
        TotalVolume = totalVolume;
        AverageAmount = averageAmount;
        AnomalyCount = anomalyCount;
        AnomalyRate = transactionCount > 0 ? (double)anomalyCount / transactionCount : 0.0;
    }
}
