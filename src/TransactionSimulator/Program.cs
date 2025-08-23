using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionSimulator;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions;
using TransactionSimulator.Messaging;
using TransactionSimulator.Generation;
using Confluent.Kafka;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        RunTimeEnvironment runTimeEnv = DetectAndConfigureEnvironment(builder);

        builder.Services.AddSingleton<ITransactionGenerator, TransactionGenerator>();
        builder.Services.AddHostedService<Simulator>();

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            ConfigureProductionServices(builder);
        }
        else
        {
            ConfigureDevelopmentServices(builder);
        }

        var host = builder.Build();
        await host.RunAsync();
    }

    /// <summary>
    /// Detects the runtime environment
    /// </summary>
    private static RunTimeEnvironment DetectAndConfigureEnvironment(IHostApplicationBuilder builder)
    {
        var environmentString = builder.Configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var runTimeEnv = RunTimeEnvironmentExtensions.FromString(environmentString);

        Console.WriteLine($"Running simulator program in environment: {runTimeEnv}");
        return runTimeEnv;
    }

    /// <summary>
    /// Configures services for Production environment (Azure EventHubs + Application Insights)
    /// </summary>
    private static void ConfigureProductionServices(IHostApplicationBuilder builder)
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


        builder.Services.AddOptions<MessagingSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.EventHubsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<ApplicationInsightsSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IMessageProducer<Null, string>, EventHubsProducer>();
        builder.Services.AddApplicationInsightsTelemetryWorkerService();
    }

    /// <summary>
    /// Configures services for Development/Local environment (Kafka)
    /// </summary>
    private static void ConfigureDevelopmentServices(IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<KafkaSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.KafkaConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IMessageProducer<Null, string>, KafkaProducer>();
    }
}
