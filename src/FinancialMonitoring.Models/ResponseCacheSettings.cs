using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// Configuration settings for HTTP response caching infrastructure.
/// These settings control memory usage and behavior for server-side response caching.
/// </summary>
public class ResponseCacheSettings
{
    /// <summary>
    /// Maximum size of a single response body that can be cached, in megabytes.
    /// Default: 1 MB - Prevents large responses from consuming excessive cache memory.
    /// </summary>
    [Range(1, 100)]
    public int MaximumBodySizeMB { get; set; } = 1;

    /// <summary>
    /// Total size limit for the response cache, in megabytes.
    /// Default: 10 MB - Reasonable limit for API response caching.
    /// </summary>
    [Range(1, 1000)]
    public int SizeLimitMB { get; set; } = 10;

    /// <summary>
    /// Whether cache keys should be case-sensitive.
    /// Default: false - More forgiving for URL variations.
    /// </summary>
    public bool UseCaseSensitivePaths { get; set; } = false;

    /// <summary>
    /// Gets the maximum body size in bytes for internal use.
    /// </summary>
    public long MaximumBodySizeBytes => MaximumBodySizeMB * 1024L * 1024L;

    /// <summary>
    /// Gets the size limit in bytes for internal use.
    /// </summary>
    public long SizeLimitBytes => SizeLimitMB * 1024L * 1024L;
}
