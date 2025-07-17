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

// Load configuration from Azure Key Vault if the URI is provided.
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
        // Log errors but allow the application to continue, as secrets might be available from other sources.
        Console.WriteLine($"Error connecting to Azure Key Vault: {ex.Message}");
    }
}
else
{
    Console.WriteLine("KEY_VAULT_URI not configured. Key Vault secrets will not be loaded.");
}

// Bind configuration sections to strongly-typed settings objects.
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

// Configure the anomaly detection mode based on the "AnomalyDetection:Mode" configuration value.
var anomalyDetectionMode = builder.Configuration["AnomalyDetection:Mode"]?.ToLowerInvariant() ?? "stateless";
Console.WriteLine($"Configuring anomaly detection mode: {anomalyDetectionMode}");

// Only register RedisSettings if using stateful mode
if (anomalyDetectionMode == "stateful")
{
    builder.Services.AddOptions<RedisSettings>()
        .Bind(builder.Configuration.GetSection(AppConstants.RedisDbConfigPrefix))
        .ValidateDataAnnotations()
        .ValidateOnStart();
}

builder.Services.AddOptions<AnomalyDetectionSettings>()
    .Bind(builder.Configuration.GetSection("AnomalyDetection"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure the messaging provider based on the "Messaging:Provider" configuration value.
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

// Configure core application services.
builder.Services.AddApplicationInsightsTelemetryWorkerService();

// Register application services with the dependency injection container.
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

// Configure anomaly detection based on mode
if (anomalyDetectionMode == "stateful")
{
    Console.WriteLine("Configuring stateful anomaly detection with Redis dependency");
    builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
    builder.Services.AddScoped<ITransactionAnomalyDetector, StatefulAnomalyDetector>();
}
else
{
    Console.WriteLine("Configuring stateless anomaly detection (no Redis dependency)");
    builder.Services.AddScoped<ITransactionAnomalyDetector, AnomalyDetector>();
}

// This hosted service ensures the Cosmos DB database and container exist before the main worker starts.
builder.Services.AddHostedService<CosmosDbInitializerHostedService>();
builder.Services.AddSingleton<IAnomalyEventPublisher, EventHubsAnomalyEventPublisher>();

// The main background service that processes transactions.
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

/// <summary>
/// A hosted service responsible for initializing the Cosmos DB database and container at application startup.
/// </summary>
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

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cosmos DB Initializer Hosted Service starting initialization.");
        await _cosmosDbService.InitializeDatabaseAsync();
        _logger.LogInformation("Cosmos DB Initializer Hosted Service has completed startup tasks.");
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cosmos DB Initializer Hosted Service stopping.");
        return Task.CompletedTask;
    }
}
