using TransactionProcessor;
using TransactionProcessor.Services;
using FinancialMonitoring.Abstractions.Persistence;
using TransactionProcessor.AnomalyDetection;
using FinancialMonitoring.Abstractions.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CosmosDbSettings>(
    //Take settings from appsettings
    builder.Configuration.GetSection("CosmosDb")
);

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
