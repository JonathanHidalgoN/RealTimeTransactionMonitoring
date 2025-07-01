namespace FinancialMonitoring.Models;

/// <summary>
/// Holds configuration settings for the anomaly detection service.
/// </summary>
public class AnomalyDetectionSettings
{
    /// <summary>
    /// The prefix for the Redis key used to store account statistics.
    /// </summary>
    public string AccountStatsKeyPrefix { get; set; } = "account-stats:";

    /// <summary>
    /// The minimum number of transactions an account must have before anomaly detection is applied.
    /// </summary>
    public int MinimumTransactionCount { get; set; } = 5;

    /// <summary>
    /// The factor by which a transaction amount must exceed the account's average to be considered an anomaly.
    /// </summary>
    public double HighValueDeviationFactor { get; set; } = 10.0;

    /// <summary>
    /// The string identifier used to flag a high-value deviation anomaly.
    /// </summary>
    public string HighValueDeviationAnomalyFlag { get; set; } = "HighValueDeviationAnomaly";
}
