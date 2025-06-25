using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using System.Net;

namespace FinancialMonitoring.Api.Services;

public class CosmosDbTransactionQueryService : ITransactionQueryService, IAsyncDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbSettings _settings;
    private readonly ILogger<CosmosDbTransactionQueryService> _logger;
    private Container? _container;

    public CosmosDbTransactionQueryService(IOptions<CosmosDbSettings> cosmosDbSettings, ILogger<CosmosDbTransactionQueryService> logger)
    {
        _settings = cosmosDbSettings.Value;
        _logger = logger;

        _logger.LogInformation("Initializing CosmosDbTransactionQueryService for endpoint {EndpointUri}", _settings.EndpointUri);
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

    private async Task EnsureContainerInitializedAsync()
    {
        if (_container == null)
        {
            try
            {
                var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);
                _container = database.Database.GetContainer(_settings.ContainerName);
                _logger.LogInformation("Container '{ContainerName}' reference obtained.", _settings.ContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining Cosmos DB container reference.");
                throw;
            }
        }
    }


    public async Task<PagedResult<Transaction>?> GetAllTransactionsAsync(int pageNumber = 1, int pageSize = 20)
    {
        await EnsureContainerInitializedAsync();
        if (_container == null) return null;

        _logger.LogInformation("Fetching transactions, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
        var countIterator = _container.GetItemQueryIterator<int>(countQuery);
        var countResponse = await countIterator.ReadNextAsync();
        var totalCount = countResponse.Resource.First();

        var offset = (pageNumber - 1) * pageSize;
        var dataQuery = new QueryDefinition("SELECT * FROM c ORDER BY c.Timestamp DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", offset)
            .WithParameter("@limit", pageSize);

        var results = new List<TransactionForCosmos>();
        using (var feed = _container.GetItemQueryIterator<TransactionForCosmos>(dataQuery))
        {
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                results.AddRange(response.ToList());
            }
        }

        return new PagedResult<Transaction>
        {
            Items = results.Select(t => t.ToTransaction()).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        await EnsureContainerInitializedAsync();
        if (_container == null) return null;

        _logger.LogInformation("Fetching transaction by ID: {Id}", id);
        try
        {
            ItemResponse<Transaction> response = await _container.ReadItemAsync<Transaction>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Transaction with ID: {Id} not found.", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transaction by ID: {Id}", id);
            throw;
        }
    }


    public async Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize)
    {
        await EnsureContainerInitializedAsync();
        if (_container == null) return null;

        _logger.LogInformation("Fetching anomalous transactions, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        const string anomalyFilter = " WHERE c.AnomalyFlag != null";

        var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c{anomalyFilter}");
        var countIterator = _container.GetItemQueryIterator<int>(countQuery);
        var countResponse = await countIterator.ReadNextAsync();
        var totalCount = countResponse.Resource.First();

        var offset = (pageNumber - 1) * pageSize;
        var dataQuery = new QueryDefinition($"SELECT * FROM c{anomalyFilter} ORDER BY c.Timestamp DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", offset)
            .WithParameter("@limit", pageSize);

        var results = new List<TransactionForCosmos>();
        using (var feed = _container.GetItemQueryIterator<TransactionForCosmos>(dataQuery))
        {
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                results.AddRange(response.ToList());
            }
        }

        return new PagedResult<Transaction>
        {
            Items = results.Select(t => t.ToTransaction()).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing CosmosClient in QueryService.");
        _cosmosClient?.Dispose();
        await ValueTask.CompletedTask;
    }
}
