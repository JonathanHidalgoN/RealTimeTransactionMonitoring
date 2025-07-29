using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using FinancialMonitoring.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FinancialMonitoring.Abstractions.Persistence;

public class CosmosDbService : ICosmosDbService, IAsyncDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbSettings _settings;
    private readonly ILogger<CosmosDbService> _logger;
    private Database? _database;
    private Container? _container;

    public CosmosDbService(IOptions<CosmosDbSettings> cosmosDbSettings, ILogger<CosmosDbService> logger)
    {
        _settings = cosmosDbSettings.Value;
        _logger = logger;

        _logger.LogInformation("Attempting to connect to Cosmos DB at {EndpointUri}", _settings.EndpointUri);

        var clientOptions = new CosmosClientOptions
        {
            HttpClientFactory = () =>
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(httpMessageHandler);
            },
            ConnectionMode = ConnectionMode.Gateway
        };

        _cosmosClient = new CosmosClient(_settings.EndpointUri, _settings.PrimaryKey, clientOptions);
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Ensuring database '{DatabaseName}' exists...", _settings.DatabaseName);
            _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);
            _logger.LogInformation("Database '{DatabaseName}' ensured.", _settings.DatabaseName);

            _logger.LogInformation("Ensuring container '{ContainerName}' exists in database '{DatabaseName}' with partition key '{PartitionKeyPath}'...",
                _settings.ContainerName, _settings.DatabaseName, _settings.PartitionKeyPath);
            // Ensure the partition key path starts with a '/'
            string partitionKeyPath = _settings.PartitionKeyPath.StartsWith("/") ? _settings.PartitionKeyPath : "/" + _settings.PartitionKeyPath;

            _container = await _database.CreateContainerIfNotExistsAsync(
                _settings.ContainerName,
                partitionKeyPath
            );
            _logger.LogInformation("Container '{ContainerName}' ensured.", _settings.ContainerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Cosmos DB database or container.");
            throw;
        }
    }

    public async Task AddTransactionAsync(TransactionForCosmos item)
    {
        if (_container == null)
        {
            _logger.LogError("Cosmos DB container is not initialized. Call InitializeDatabaseAsync first.");
            throw new InvalidOperationException("Container not initialized.");
        }

        try
        {
            string jsonToUpsert = JsonSerializer.Serialize(item);
            _logger.LogInformation("Attempting to upsert item with id '{ItemId}' to Cosmos DB. JSON Payload: {JsonPayload}", item.id, jsonToUpsert);
            var response = await _container.UpsertItemAsync(
                item,
                new PartitionKey(item.id)
            );
            _logger.LogInformation("Upserted item to Cosmos DB. Id: {Id}, RU Charge: {RU}", response.Resource.id, response.RequestCharge);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB error while adding transaction. Status Code: {StatusCode}", ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generic error while adding transaction to Cosmos DB.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing CosmosClient.");
        _cosmosClient?.Dispose();
        await ValueTask.CompletedTask; // CosmosClient.Dispose is synchronous
    }
}
