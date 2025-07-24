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

        builder.Services.AddOptions<MessagingSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.EventHubsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddOptions<ApplicationInsightsSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var messagingProvider = builder.Configuration["Messaging:Provider"]?.ToLowerInvariant() ?? AppConstants.KafkaDefaultName;
        Console.WriteLine($"Configuring messaging provider: {messagingProvider}");

        if (messagingProvider == "eventhubs")
        {
            builder.Services.AddOptions<EventHubsSettings>()
                .Bind(builder.Configuration.GetSection(AppConstants.EventHubsConfigPrefix))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddSingleton<IMessageProducer<Null, string>, EventHubsProducer>();
        }
        else
        {
            builder.Services.AddOptions<KafkaSettings>()
                .Bind(builder.Configuration.GetSection(AppConstants.KafkaConfigPrefix))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddSingleton<IMessageProducer<Null, string>, KafkaProducer>();
        }

        builder.Services.AddApplicationInsightsTelemetryWorkerService();

        builder.Services.AddSingleton<ITransactionGenerator, TransactionGenerator>();

        builder.Services.AddHostedService<Simulator>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
