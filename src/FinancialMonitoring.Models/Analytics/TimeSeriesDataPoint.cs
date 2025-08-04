using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models.Analytics;

/// <summary>
/// Represents a single data point in a time series for analytics.
/// </summary>
public record TimeSeriesDataPoint
{
    /// <summary>
    /// Timestamp for this data point.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    /// <summary>
    /// The value at this timestamp.
    /// </summary>
    [JsonPropertyName("value")]
    public double Value { get; init; }

    /// <summary>
    /// Additional metadata for this data point.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }

    public TimeSeriesDataPoint(long timestamp, double value, Dictionary<string, object>? metadata = null)
    {
        Timestamp = timestamp;
        Value = value;
        Metadata = metadata;
    }
}
