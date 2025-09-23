using FinancialMonitoring.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace TransactionSimulator.Extensions.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateConfiguration(IConfiguration configuration, RunTimeEnvironment environment)
    {
        var errors = new List<string>();

        if (environment == RunTimeEnvironment.Production)
        {
            ValidateSection<MessagingSettings>(configuration, AppConstants.EventHubsConfigPrefix, errors);
            ValidateSection<ApplicationInsightsSettings>(configuration, AppConstants.ApplicationInsightsConfigPrefix, errors);

            if (string.IsNullOrEmpty(configuration["KEY_VAULT_URI"]))
            {
                errors.Add("KEY_VAULT_URI is required for Production environment");
            }
        }
        else
        {
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