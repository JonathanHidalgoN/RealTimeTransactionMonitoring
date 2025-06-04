using TransactionProcessor;
using TransactionProcessor.Services;
using FinancialMonitoring.Abstractions.Persistence;
using TransactionProcessor.AnomalyDetection;
using FinancialMonitoring.Abstractions.Services;
using Azure.Identity;
using FinancialMonitoring.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(AppConstants.KafkaConfigPrefix));
builder.Services.Configure<ApplicationInsightsSettings>(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix));
builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection(AppConstants.CosmosDbConfigPrefix));
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

//Take configs, declare as singleton 
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<ITransactionAnomalyDetector, AnomalyDetector>();
//Hosted to control how to end
builder.Services.AddHostedService<CosmosDbInitializerHostedService>();
builder.Services.AddHostedService<Worker>();

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
