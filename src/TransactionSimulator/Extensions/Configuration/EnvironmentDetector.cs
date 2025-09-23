using Azure.Identity;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TransactionSimulator.Extensions.Configuration;

public static class EnvironmentDetector
{
    public static RunTimeEnvironment DetectAndConfigureEnvironment(IHostApplicationBuilder builder)
    {
        var environmentString = builder.Configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var runTimeEnv = RunTimeEnvironmentExtensions.FromString(environmentString);

        Console.WriteLine($"Running simulator program in environment: {runTimeEnv}");

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            ConfigureKeyVault(builder);
        }

        return runTimeEnv;
    }

    private static void ConfigureKeyVault(IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];

        if (!string.IsNullOrEmpty(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var vaultUri))
        {
            Console.WriteLine($"Attempting to load configuration from Azure Key Vault: {vaultUri}");
            try
            {
                builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
                Console.WriteLine("Successfully configured to load secrets from Azure Key Vault.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Azure Key Vault: {ex.Message}");
                throw;
            }
        }
        else
        {
            throw new ArgumentException("KEY_VAULT_URI environment variable is required for Production runtime");
        }
    }
}