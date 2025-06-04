using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionSimulator;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Models;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(AppConstants.KafkaConfigPrefix));
        builder.Services.Configure<ApplicationInsightsSettings>(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix));

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
            }
        }
        else
        {
            Console.WriteLine("KEY_VAULT_URI not configured. Key Vault secrets will not be loaded.");
        }

        builder.Services.AddApplicationInsightsTelemetryWorkerService();

        builder.Services.AddHostedService<Simulator>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
