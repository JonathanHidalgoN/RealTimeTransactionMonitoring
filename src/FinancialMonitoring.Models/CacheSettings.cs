using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// Configuration settings for output cache policies.
/// Setting any value to 0 disables caching for that policy (zero overhead).
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Cache duration for transaction list endpoints in seconds.
    /// Default: 0 (disabled) - Real-time transaction data should not be cached.
    /// </summary>
    [Range(0, 3600)]
    public int TransactionCacheSeconds { get; set; } = 0;

    /// <summary>
    /// Cache duration for individual transaction lookups by ID in seconds.
    /// Default: 0 (disabled) - Individual transactions rarely change, but keeping consistent.
    /// </summary>
    [Range(0, 3600)]
    public int TransactionByIdCacheSeconds { get; set; } = 0;

    /// <summary>
    /// Cache duration for anomalous transaction queries in seconds.
    /// Default: 0 (disabled) - Anomaly detection results should be real-time.
    /// </summary>
    [Range(0, 3600)]
    public int AnomalousTransactionCacheSeconds { get; set; } = 0;

    /// <summary>
    /// Cache duration for analytics and aggregated data in seconds.
    /// Default: 300 (5 minutes) - Analytics can tolerate some staleness for performance.
    /// </summary>
    [Range(0, 3600)]
    public int AnalyticsCacheSeconds { get; set; } = 300;

    /// <summary>
    /// Cache duration for time series data in seconds.
    /// Default: 0 (disabled) - Time series should reflect latest data points.
    /// </summary>
    [Range(0, 3600)]
    public int TimeSeriesCacheSeconds { get; set; } = 0;

    /// <summary>
    /// Base cache policy duration for endpoints without specific policies in seconds.
    /// Default: 0 (disabled) - No default caching to ensure real-time data.
    /// </summary>
    [Range(0, 3600)]
    public int BasePolicyCacheSeconds { get; set; } = 0;
}