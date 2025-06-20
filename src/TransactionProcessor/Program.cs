using TransactionProcessor;
using TransactionProcessor.Services;
using FinancialMonitoring.Abstractions.Persistence;
using TransactionProcessor.AnomalyDetection;
using FinancialMonitoring.Abstractions.Services;
using Azure.Identity;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Messaging;
using TransactionProcessor.Messaging;
using Confluent.Kafka;
using FinancialMonitoring.Abstractions.Caching;
using TransactionProcessor.Caching;

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
builder.Services.AddOptions<CosmosDbSettings>()
    .Bind(builder.Configuration.GetSection(AppConstants.CosmosDbConfigPrefix))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<RedisSettings>()
    .Bind(builder.Configuration.GetSection(AppConstants.RedisDbConfigPrefix))
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
    builder.Services.AddSingleton<IMessageConsumer<Null, string>, EventHubsConsumer>();
}
else
{
    builder.Services.AddOptions<KafkaSettings>()
        .Bind(builder.Configuration.GetSection(AppConstants.KafkaConfigPrefix))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    builder.Services.AddSingleton<IMessageConsumer<Null, string>, KafkaConsumer>();
}

builder.Services.AddApplicationInsightsTelemetryWorkerService();

//Take configs, declare as singleton 
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<ITransactionAnomalyDetector, StatefulAnomalyDetector>();
//Hosted to control how to end
builder.Services.AddHostedService<CosmosDbInitializerHostedService>();
builder.Services.AddSingleton<IAnomalyEventPublisher, EventHubsAnomalyEventPublisher>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

var host = builder.Build();
host.Run();

public class CosmosDbInitializerHostedService : IHostedService
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<CosmosDbInitializerHostedService> _logger;

    public CosmosDbInitializerHostedService(
        ICosmosDbService cosmosDbService,
        ILogger<CosmosDbInitializerHostedService> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cosmos DB Initializer Hosted Service starting initialization.");
        await _cosmosDbService.InitializeDatabaseAsync();
        _logger.LogInformation("Cosmos DB Initializer Hosted Service has completed startup tasks.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cosmos DB Initializer Hosted Service stopping.");
        return Task.CompletedTask;
    }
}
