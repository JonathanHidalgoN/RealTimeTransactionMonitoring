using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models.Extensions;

/// <summary>
/// Extension methods for configuration management
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Builds port settings from environment variables with fallback to defaults.
    /// Uses data annotation validation to ensure port ranges are valid.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>Configured port settings</returns>
    public static PortSettings BuildPortSettings(this IConfiguration configuration)
    {
        var settings = new PortSettings
        {
            Api = int.TryParse(configuration["API_PORT"], out var apiPort) ? apiPort : AppConstants.DefaultApiPort,
            BlazorHttp = int.TryParse(configuration["BLAZOR_HTTP_PORT"], out var blazorHttp) ? blazorHttp : AppConstants.DefaultBlazorHttpPort,
            BlazorHttps = int.TryParse(configuration["BLAZOR_HTTPS_PORT"], out var blazorHttps) ? blazorHttps : AppConstants.DefaultBlazorHttpsPort,
            MongoDb = int.TryParse(configuration["MONGODB_PORT"], out var mongoPort) ? mongoPort : AppConstants.DefaultMongoDbPort
        };

        ValidatePortSettings(settings);
        return settings;
    }

    private static void ValidatePortSettings(PortSettings settings)
    {
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(settings, context, results, true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Port configuration validation failed: {errors}");
        }
    }
}
