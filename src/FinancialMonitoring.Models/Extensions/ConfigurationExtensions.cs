using Microsoft.Extensions.Configuration;

namespace FinancialMonitoring.Models.Extensions;

/// <summary>
/// Extension methods for configuration management
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Builds port settings from environment variables with fallback to defaults
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>Configured port settings</returns>
    public static PortSettings BuildPortSettings(this IConfiguration configuration)
    {
        return new PortSettings
        {
            Api = int.TryParse(configuration["API_PORT"], out var apiPort) ? apiPort : AppConstants.DefaultApiPort,
            BlazorHttp = int.TryParse(configuration["BLAZOR_HTTP_PORT"], out var blazorHttp) ? blazorHttp : AppConstants.DefaultBlazorHttpPort,
            BlazorHttps = int.TryParse(configuration["BLAZOR_HTTPS_PORT"], out var blazorHttps) ? blazorHttps : AppConstants.DefaultBlazorHttpsPort,
            MongoDb = int.TryParse(configuration["MONGODB_PORT"], out var mongoPort) ? mongoPort : AppConstants.DefaultMongoDbPort
        };
    }
}
