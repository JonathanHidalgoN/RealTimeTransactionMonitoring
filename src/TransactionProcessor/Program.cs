using TransactionProcessor;
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

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        RunTimeEnvironment runTimeEnv = DetectAndConfigureEnvironment(builder);

        // Add common services
        builder.Services.AddHostedService<DatabaseInitializerHostedService>();
        builder.Services.AddHostedService<Worker>();

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            ConfigureProductionServices(builder);
        }
        else
        {
            ConfigureDevelopmentServices(builder);
        }

        // Configure anomaly detection
        ConfigureAnomalyDetection(builder);

        var host = builder.Build();
        host.Run();
    }

    /// <summary>
    /// Detects the runtime environment and configures Azure Key Vault for Production
    /// </summary>
    private static RunTimeEnvironment DetectAndConfigureEnvironment(IHostApplicationBuilder builder)
    {
        var environmentString = builder.Configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var runTimeEnv = RunTimeEnvironmentExtensions.FromString(environmentString);

        Console.WriteLine($"Running processor program in environment: {runTimeEnv}");

        if (runTimeEnv == RunTimeEnvironment.Production)
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

        return runTimeEnv;
    }

    /// <summary>
    /// Configures services for Production environment (Azure CosmosDB, EventHubs, Application Insights)
    /// </summary>
    private static void ConfigureProductionServices(IHostApplicationBuilder builder)
    {
        // Configure Azure services settings
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

        // Configure messaging (EventHubs)
        builder.Services.AddOptions<EventHubsSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.EventHubsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddSingleton<IMessageConsumer<Null, string>, EventHubsConsumer>();

        // Configure repository (CosmosDB)
        Console.WriteLine("Configuring Cosmos DB repository for production");
        builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
        builder.Services.AddSingleton<ITransactionRepository, CosmosTransactionRepository>();

        // Configure anomaly event publisher (EventHubs)
        Console.WriteLine("Using Azure Event Hubs anomaly event publisher for production");
        builder.Services.AddSingleton<IAnomalyEventPublisher, EventHubsAnomalyEventPublisher>();

        // Add Application Insights
        builder.Services.AddApplicationInsightsTelemetryWorkerService();
    }

    /// <summary>
    /// Configures services for Development/Local environment (MongoDB, Kafka)
    /// </summary>
    private static void ConfigureDevelopmentServices(IHostApplicationBuilder builder)
    {
        // Configure local database settings
        builder.Services.AddOptions<MongoDbSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.MongoDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure messaging (Kafka)
        builder.Services.AddOptions<KafkaSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.KafkaConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddSingleton<IMessageConsumer<Null, string>, KafkaConsumer>();

        // Configure repository (MongoDB)
        Console.WriteLine("Configuring MongoDB repository for local development/testing");
        builder.Services.AddSingleton<ITransactionRepository, MongoTransactionRepository>();

        // Configure anomaly event publisher (NoOp for local)
        Console.WriteLine("Using NoOp anomaly event publisher for local development");
        builder.Services.AddSingleton<IAnomalyEventPublisher, NoOpAnomalyEventPublisher>();
    }

    /// <summary>
    /// Configures anomaly detection services based on the configured mode (stateful/stateless)
    /// </summary>
    private static void ConfigureAnomalyDetection(IHostApplicationBuilder builder)
    {
        var anomalyDetectionMode = builder.Configuration["AnomalyDetection:Mode"]?.ToLowerInvariant() ?? "stateless";
        Console.WriteLine($"Configuring anomaly detection mode: {anomalyDetectionMode}");

        builder.Services.AddOptions<AnomalyDetectionSettings>()
            .Bind(builder.Configuration.GetSection("AnomalyDetection"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (anomalyDetectionMode == "stateful")
        {
            Console.WriteLine("Configuring stateful anomaly detection with Redis dependency");
            builder.Services.AddOptions<RedisSettings>()
                .Bind(builder.Configuration.GetSection(AppConstants.RedisDbConfigPrefix))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
            builder.Services.AddScoped<ITransactionAnomalyDetector, StatefulAnomalyDetector>();
        }
        else
        {
            Console.WriteLine("Configuring stateless anomaly detection (no Redis dependency)");
            builder.Services.AddScoped<ITransactionAnomalyDetector, AnomalyDetector>();
        }
    }
}

/// <summary>
/// A hosted service responsible for initializing the database and its containers/collections at application startup.
/// </summary>
public class DatabaseInitializerHostedService : IHostedService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        ITransactionRepository transactionRepository,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database Initializer Hosted Service starting initialization.");
        await _transactionRepository.InitializeAsync();
        _logger.LogInformation("Database Initializer Hosted Service has completed startup tasks.");
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database Initializer Hosted Service stopping.");
        return Task.CompletedTask;
    }
}
