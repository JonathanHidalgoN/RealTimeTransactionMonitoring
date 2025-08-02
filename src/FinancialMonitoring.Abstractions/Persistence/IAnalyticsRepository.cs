using FinancialMonitoring.Models.Analytics;

namespace FinancialMonitoring.Abstractions.Persistence;

/// <summary>
/// Repository interface for analytics operations.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Gets global transaction analytics and statistics.
    /// </summary>
    /// <returns>Global transaction analytics.</returns>
    Task<TransactionAnalytics> GetTransactionAnalyticsAsync();

    /// <summary>
    /// Gets time series data for transaction counts over a specified period.
    /// </summary>
    /// <param name="fromTimestamp">Start timestamp (Unix milliseconds).</param>
    /// <param name="toTimestamp">End timestamp (Unix milliseconds).</param>
    /// <param name="intervalMinutes">Interval in minutes for data points.</param>
    /// <returns>List of time series data points.</returns>
    Task<List<TimeSeriesDataPoint>> GetTransactionTimeSeriesAsync(long fromTimestamp, long toTimestamp, int intervalMinutes = 60);

    /// <summary>
    /// Gets time series data for anomaly counts over a specified period.
    /// </summary>
    /// <param name="fromTimestamp">Start timestamp (Unix milliseconds).</param>
    /// <param name="toTimestamp">End timestamp (Unix milliseconds).</param>
    /// <param name="intervalMinutes">Interval in minutes for data points.</param>
    /// <returns>List of time series data points.</returns>
    Task<List<TimeSeriesDataPoint>> GetAnomalyTimeSeriesAsync(long fromTimestamp, long toTimestamp, int intervalMinutes = 60);

    /// <summary>
    /// Gets analytics data for top merchants by transaction volume.
    /// </summary>
    /// <param name="topCount">Number of top merchants to return.</param>
    /// <returns>List of merchant analytics.</returns>
    Task<List<MerchantAnalytics>> GetTopMerchantsAnalyticsAsync(int topCount = 10);

    /// <summary>
    /// Gets analytics data grouped by merchant category.
    /// </summary>
    /// <returns>List of merchant analytics grouped by category.</returns>
    Task<List<MerchantAnalytics>> GetMerchantCategoryAnalyticsAsync();
}