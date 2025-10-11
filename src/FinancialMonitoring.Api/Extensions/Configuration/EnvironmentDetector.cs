using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Extensions;

namespace FinancialMonitoring.Api.Extensions.Configuration;

public static class EnvironmentDetector
{
    public static RunTimeEnvironment DetectAndConfigureEnvironment(
        WebApplicationBuilder builder,
        IKeyVaultConfigurer? keyVaultConfigurer = null)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("EnvironmentDetector");

        var environmentString = builder.Environment.EnvironmentName;
        var runTimeEnv = RunTimeEnvironmentExtensions.FromString(environmentString);

        logger.LogInformation("Running API program in environment: {Environment}", runTimeEnv);

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            keyVaultConfigurer ??= new AzureKeyVaultConfigurer();
            ConfigureKeyVault(builder, logger, keyVaultConfigurer);
        }

        LogPortConfiguration(builder.Configuration, logger);

        return runTimeEnv;
    }

    private static void ConfigureKeyVault(WebApplicationBuilder builder, ILogger logger, IKeyVaultConfigurer keyVaultConfigurer)
    {
        var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];

        if (!string.IsNullOrEmpty(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var vaultUri))
        {
            logger.LogInformation("Attempting to load configuration from Azure Key Vault: {KeyVaultUri}", vaultUri);
            try
            {
                keyVaultConfigurer.ConfigureKeyVault(builder.Configuration, vaultUri);
                logger.LogInformation("Successfully configured to load secrets from Azure Key Vault");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error connecting to Azure Key Vault: {KeyVaultUri}", vaultUri);
                throw;
            }
        }
        else
        {
            throw new ArgumentException("KEY_VAULT_URI environment variable is required for Production runtime");
        }
    }

    private static void LogPortConfiguration(IConfiguration configuration, ILogger logger)
    {
        var portSettings = configuration.BuildPortSettings();

        logger.LogInformation("Port Configuration: API={ApiPort} (from {ApiPortSource}), Blazor HTTP={BlazorHttpPort} (from {BlazorHttpSource}), Blazor HTTPS={BlazorHttpsPort} (from {BlazorHttpsSource}), MongoDB={MongoDbPort} (from {MongoDbSource})",
            portSettings.Api, configuration["API_PORT"] != null ? "environment" : "default",
            portSettings.BlazorHttp, configuration["BLAZOR_HTTP_PORT"] != null ? "environment" : "default",
            portSettings.BlazorHttps, configuration["BLAZOR_HTTPS_PORT"] != null ? "environment" : "default",
            portSettings.MongoDb, configuration["MONGODB_PORT"] != null ? "environment" : "default");
    }
}
