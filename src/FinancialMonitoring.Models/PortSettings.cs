using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

/// <summary>
/// Configuration settings for application ports
/// </summary>
public class PortSettings
{
    /// <summary>
    /// Port for the Financial Monitoring API
    /// </summary>
    [Range(1024, 65535, ErrorMessage = "API port must be between 1024 and 65535")]
    public int Api { get; set; } = AppConstants.DefaultApiPort;

    /// <summary>
    /// HTTP port for the Blazor WebAssembly application
    /// </summary>
    [Range(1024, 65535, ErrorMessage = "Blazor HTTP port must be between 1024 and 65535")]
    public int BlazorHttp { get; set; } = AppConstants.DefaultBlazorHttpPort;

    /// <summary>
    /// HTTPS port for the Blazor WebAssembly application
    /// </summary>
    [Range(1024, 65535, ErrorMessage = "Blazor HTTPS port must be between 1024 and 65535")]
    public int BlazorHttps { get; set; } = AppConstants.DefaultBlazorHttpsPort;

    /// <summary>
    /// Port for MongoDB database
    /// </summary>
    [Range(1024, 65535, ErrorMessage = "MongoDB port must be between 1024 and 65535")]
    public int MongoDb { get; set; } = AppConstants.DefaultMongoDbPort;
}
