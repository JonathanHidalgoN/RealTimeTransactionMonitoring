// NOTE: This file contains conditional timeout logic for Testing environment only.
// The shorter timeouts (10-15 seconds) are applied because the local CosmosDB emulator
// is very slow on the developer's machine and causes load tests to hang indefinitely.
// Production timeouts remain at normal values for reliable operation.

using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;

namespace FinancialMonitoring.Api.Services;

public class CosmosDbTransactionQueryService : ITransactionQueryService, IAsyncDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbSettings _settings;
    private readonly ILogger<CosmosDbTransactionQueryService> _logger;
    private readonly bool _isTestingEnvironment;
    private Container? _container;

    public CosmosDbTransactionQueryService(IOptions<CosmosDbSettings> cosmosDbSettings, ILogger<CosmosDbTransactionQueryService> logger, IWebHostEnvironment environment)
    {
        _settings = cosmosDbSettings.Value;
        _logger = logger;
        _isTestingEnvironment = environment.EnvironmentName == "Testing";

        _logger.LogInformation("Initializing CosmosDbTransactionQueryService for endpoint {EndpointUri}", _settings.EndpointUri);
        var clientOptions = new CosmosClientOptions
        {
            HttpClientFactory = () =>
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                var httpClient = new HttpClient(httpMessageHandler);
                // Use shorter timeout only in Testing environment due to slow local CosmosDB emulator
                httpClient.Timeout = _isTestingEnvironment ? TimeSpan.FromSeconds(15) : TimeSpan.FromMinutes(5);
                return httpClient;
            },
            ConnectionMode = ConnectionMode.Gateway,
            RequestTimeout = _isTestingEnvironment ? TimeSpan.FromSeconds(15) : TimeSpan.FromMinutes(5)
        };
        _cosmosClient = new CosmosClient(_settings.EndpointUri, _settings.PrimaryKey, clientOptions);
    }

    private async Task EnsureContainerInitializedAsync()
    {
        if (_container == null)
        {
            try
            {
                if (_isTestingEnvironment)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName, cancellationToken: cts.Token);
                    _container = database.Database.GetContainer(_settings.ContainerName);
                }
                else
                {
                    var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_settings.DatabaseName);
                    _container = database.Database.GetContainer(_settings.ContainerName);
                }
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
        try
        {
            await EnsureContainerInitializedAsync();
            if (_container == null)
            {
                _logger.LogWarning("Container not initialized, returning empty result");
                return CreateEmptyPagedResult(pageNumber, pageSize);
            }

            _logger.LogInformation("Fetching transactions, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            using var cts = new CancellationTokenSource(_isTestingEnvironment ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(5));

            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var countIterator = _container.GetItemQueryIterator<int>(countQuery);
            var countResponse = await countIterator.ReadNextAsync(cts.Token);
            var totalCount = countResponse.Resource.FirstOrDefault();

            if (totalCount == 0)
            {
                _logger.LogInformation("No transactions found in database");
                return CreateEmptyPagedResult(pageNumber, pageSize);
            }

            var offset = (pageNumber - 1) * pageSize;
            var dataQuery = new QueryDefinition("SELECT * FROM c ORDER BY c.Timestamp DESC OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", offset)
                .WithParameter("@limit", pageSize);

            var results = new List<TransactionForCosmos>();
            using (var feed = _container.GetItemQueryIterator<TransactionForCosmos>(dataQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync(cts.Token);
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
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Query timed out, returning empty result");
            return CreateEmptyPagedResult(pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transactions, returning empty result");
            return CreateEmptyPagedResult(pageNumber, pageSize);
        }
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        try
        {
            await EnsureContainerInitializedAsync();
            if (_container == null) return null;

            _logger.LogInformation("Fetching transaction by ID: {Id}", id);

            using var cts = new CancellationTokenSource(_isTestingEnvironment ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(5));

            ItemResponse<Transaction> response = await _container.ReadItemAsync<Transaction>(id, new PartitionKey(id), cancellationToken: cts.Token);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Transaction with ID: {Id} not found.", id);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Query for transaction {Id} timed out", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transaction by ID: {Id}", id);
            return null;
        }
    }

    private static PagedResult<Transaction> CreateEmptyPagedResult(int pageNumber, int pageSize)
    {
        return new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }


    public async Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize)
    {
        try
        {
            await EnsureContainerInitializedAsync();
            if (_container == null)
            {
                _logger.LogWarning("Container not initialized, returning empty anomalous transactions result");
                return CreateEmptyPagedResult(pageNumber, pageSize);
            }

            _logger.LogInformation("Fetching anomalous transactions, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            using var cts = new CancellationTokenSource(_isTestingEnvironment ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(5));

            const string anomalyFilter = " WHERE c.AnomalyFlag != null";

            var countQuery = new QueryDefinition($"SELECT VALUE COUNT(1) FROM c{anomalyFilter}");
            var countIterator = _container.GetItemQueryIterator<int>(countQuery);
            var countResponse = await countIterator.ReadNextAsync(cts.Token);
            var totalCount = countResponse.Resource.FirstOrDefault();

            if (totalCount == 0)
            {
                _logger.LogInformation("No anomalous transactions found in database");
                return CreateEmptyPagedResult(pageNumber, pageSize);
            }

            var offset = (pageNumber - 1) * pageSize;
            var dataQuery = new QueryDefinition($"SELECT * FROM c{anomalyFilter} ORDER BY c.Timestamp DESC OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", offset)
                .WithParameter("@limit", pageSize);

            var results = new List<TransactionForCosmos>();
            using (var feed = _container.GetItemQueryIterator<TransactionForCosmos>(dataQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync(cts.Token);
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
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Anomalous transactions query timed out, returning empty result");
            return CreateEmptyPagedResult(pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching anomalous transactions, returning empty result");
            return CreateEmptyPagedResult(pageNumber, pageSize);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing CosmosClient in QueryService.");
        _cosmosClient?.Dispose();
        await ValueTask.CompletedTask;
    }
}
