using FinancialMonitoring.Models;
using System.ComponentModel.DataAnnotations;

namespace TransactionProcessor.Extensions.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration configuration, RunTimeEnvironment environment)
    {
        if (environment == RunTimeEnvironment.Testing)
        {
            return;
        }

        var errors = new List<string>();

        ValidateSection<AnomalyDetectionSettings>(configuration, "AnomalyDetection", errors);

        if (environment == RunTimeEnvironment.Production)
        {
            ValidateSection<ApplicationInsightsSettings>(configuration, AppConstants.ApplicationInsightsConfigPrefix, errors);
            ValidateSection<CosmosDbSettings>(configuration, AppConstants.CosmosDbConfigPrefix, errors);
            ValidateSection<EventHubsSettings>(configuration, AppConstants.EventHubsConfigPrefix, errors);

            if (string.IsNullOrEmpty(configuration["KEY_VAULT_URI"]))
            {
                errors.Add("KEY_VAULT_URI is required for Production environment");
            }

            var anomalyDetectionMode = configuration["AnomalyDetection:Mode"]?.ToLowerInvariant() ?? "stateless";
            if (anomalyDetectionMode == "stateful")
            {
                ValidateSection<RedisSettings>(configuration, AppConstants.RedisDbConfigPrefix, errors);
            }
        }
        else
        {
            ValidateSection<MongoDbSettings>(configuration, AppConstants.MongoDbConfigPrefix, errors);
            ValidateSection<KafkaSettings>(configuration, AppConstants.KafkaConfigPrefix, errors);
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"Configuration validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static void ValidateSection<T>(IConfiguration configuration, string sectionName, List<string> errors) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            errors.Add($"Configuration section '{sectionName}' is missing");
            return;
        }

        var settings = section.Get<T>();
        if (settings == null)
        {
            errors.Add($"Failed to bind configuration section '{sectionName}' to type {typeof(T).Name}");
            return;
        }

        var validationContext = new ValidationContext(settings);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(settings, validationContext, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                errors.Add($"Section '{sectionName}': {validationResult.ErrorMessage}");
            }
        }
    }
}
