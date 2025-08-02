using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// Configuration settings for CORS (Cross-Origin Resource Sharing)
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// List of allowed origins for CORS requests
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of allowed HTTP headers for CORS requests
    /// </summary>
    public string[] AllowedHeaders { get; set; } = new[] { AppConstants.ApiKeyHeader };

    /// <summary>
    /// List of allowed HTTP methods for CORS requests
    /// </summary>
    public string[] AllowedMethods { get; set; } = new[] { "GET" };

    /// <summary>
    /// Whether to allow credentials in CORS requests
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Builds the allowed origins list using the provided port settings
    /// </summary>
    /// <param name="portSettings">Port configuration</param>
    /// <returns>Array of allowed origin URLs</returns>
    public static string[] BuildDefaultOrigins(PortSettings portSettings)
    {
        return new[]
        {
            $"http://localhost:{portSettings.BlazorHttp}",
            $"https://localhost:{portSettings.BlazorHttps}"
        };
    }
}