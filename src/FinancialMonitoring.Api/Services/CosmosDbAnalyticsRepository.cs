using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// Cosmos DB implementation of the analytics repository.
/// </summary>
public class CosmosDbAnalyticsRepository : IAnalyticsRepository, IAsyncDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbSettings _settings;
    private readonly ILogger<CosmosDbAnalyticsRepository> _logger;
    private Container? _container;

    public CosmosDbAnalyticsRepository(IOptions<CosmosDbSettings> cosmosDbSettings, ILogger<CosmosDbAnalyticsRepository> logger)
    {
        _settings = cosmosDbSettings.Value;
        _logger = logger;

        _logger.LogInformation("Initializing CosmosDbAnalyticsRepository for endpoint {EndpointUri}", _settings.EndpointUri);

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

    public async Task<TransactionAnalytics> GetTransactionAnalyticsAsync()
    {
        await EnsureContainerInitializedAsync();
        if (_container == null)
        {
            _logger.LogError("Container not initialized");
            throw new InvalidOperationException("Container not initialized");
        }

        _logger.LogInformation("Calculating global transaction analytics");

        try
        {
            // Calculate basic statistics
            var totalTransactionsQuery = "SELECT VALUE COUNT(1) FROM c";
            var totalTransactionsIterator = _container.GetItemQueryIterator<long>(totalTransactionsQuery);
            var totalTransactions = (await totalTransactionsIterator.ReadNextAsync()).FirstOrDefault();

            var anomaliesQuery = "SELECT VALUE COUNT(1) FROM c WHERE c.anomalyFlag != null";
            var anomaliesIterator = _container.GetItemQueryIterator<long>(anomaliesQuery);
            var totalAnomalies = (await anomaliesIterator.ReadNextAsync()).FirstOrDefault();

            var volumeQuery = "SELECT VALUE SUM(c.amount) FROM c";
            var volumeIterator = _container.GetItemQueryIterator<double>(volumeQuery);
            var totalVolume = (await volumeIterator.ReadNextAsync()).FirstOrDefault();

            var averageQuery = "SELECT VALUE AVG(c.amount) FROM c";
            var averageIterator = _container.GetItemQueryIterator<double>(averageQuery);
            var averageAmount = (await averageIterator.ReadNextAsync()).FirstOrDefault();

            // Get unique accounts count
            var uniqueAccountsQuery = "SELECT VALUE COUNT(DISTINCT c.sourceAccount.accountId) FROM c";
            var uniqueAccountsIterator = _container.GetItemQueryIterator<long>(uniqueAccountsQuery);
            var uniqueAccounts = (await uniqueAccountsIterator.ReadNextAsync()).FirstOrDefault();

            // Calculate last 24 hours statistics
            var last24Hours = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();

            var recent24HQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.timestamp >= {last24Hours}";
            var recent24HIterator = _container.GetItemQueryIterator<long>(recent24HQuery);
            var transactionsLast24Hours = (await recent24HIterator.ReadNextAsync()).FirstOrDefault();

            var recentAnomalies24HQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.timestamp >= {last24Hours} AND c.anomalyFlag != null";
            var recentAnomalies24HIterator = _container.GetItemQueryIterator<long>(recentAnomalies24HQuery);
            var anomaliesLast24Hours = (await recentAnomalies24HIterator.ReadNextAsync()).FirstOrDefault();

            return new TransactionAnalytics(
                totalTransactions,
                totalAnomalies,
                totalVolume,
                averageAmount,
                uniqueAccounts,
                transactionsLast24Hours,
                anomaliesLast24Hours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating transaction analytics");
            throw;
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetTransactionTimeSeriesAsync(long fromTimestamp, long toTimestamp, int intervalMinutes = 60)
    {
        await EnsureContainerInitializedAsync();
        if (_container == null)
        {
            _logger.LogError("Container not initialized");
            throw new InvalidOperationException("Container not initialized");
        }

        _logger.LogInformation("Getting transaction time series from {FromTimestamp} to {ToTimestamp} with {IntervalMinutes} minute intervals",
            fromTimestamp, toTimestamp, intervalMinutes);

        try
        {
            var intervalMs = intervalMinutes * 60 * 1000;
            var dataPoints = new List<TimeSeriesDataPoint>();

            for (var currentTime = fromTimestamp; currentTime < toTimestamp; currentTime += intervalMs)
            {
                var nextTime = Math.Min(currentTime + intervalMs, toTimestamp);

                var query = $"SELECT VALUE COUNT(1) FROM c WHERE c.timestamp >= {currentTime} AND c.timestamp < {nextTime}";
                var iterator = _container.GetItemQueryIterator<long>(query);
                var count = (await iterator.ReadNextAsync()).FirstOrDefault();

                dataPoints.Add(new TimeSeriesDataPoint(currentTime, count));
            }

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction time series");
            throw;
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetAnomalyTimeSeriesAsync(long fromTimestamp, long toTimestamp, int intervalMinutes = 60)
    {
        await EnsureContainerInitializedAsync();
        if (_container == null)
        {
            _logger.LogError("Container not initialized");
            throw new InvalidOperationException("Container not initialized");
        }

        _logger.LogInformation("Getting anomaly time series from {FromTimestamp} to {ToTimestamp} with {IntervalMinutes} minute intervals",
            fromTimestamp, toTimestamp, intervalMinutes);

        try
        {
            var intervalMs = intervalMinutes * 60 * 1000;
            var dataPoints = new List<TimeSeriesDataPoint>();

            for (var currentTime = fromTimestamp; currentTime < toTimestamp; currentTime += intervalMs)
            {
                var nextTime = Math.Min(currentTime + intervalMs, toTimestamp);

                var query = $"SELECT VALUE COUNT(1) FROM c WHERE c.timestamp >= {currentTime} AND c.timestamp < {nextTime} AND c.anomalyFlag != null";
                var iterator = _container.GetItemQueryIterator<long>(query);
                var count = (await iterator.ReadNextAsync()).FirstOrDefault();

                dataPoints.Add(new TimeSeriesDataPoint(currentTime, count));
            }

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting anomaly time series");
            throw;
        }
    }

    public async Task<List<MerchantAnalytics>> GetTopMerchantsAnalyticsAsync(int topCount = 10)
    {
        await EnsureContainerInitializedAsync();
        if (_container == null)
        {
            _logger.LogError("Container not initialized");
            throw new InvalidOperationException("Container not initialized");
        }

        _logger.LogInformation("Getting top {TopCount} merchants analytics", topCount);

        try
        {
            var query = $@"
                SELECT 
                    c.merchantName,
                    c.merchantCategory,
                    COUNT(1) as transactionCount,
                    SUM(c.amount) as totalVolume,
                    AVG(c.amount) as averageAmount,
                    SUM(CASE WHEN c.anomalyFlag != null THEN 1 ELSE 0 END) as anomalyCount
                FROM c 
                GROUP BY c.merchantName, c.merchantCategory 
                ORDER BY SUM(c.amount) DESC 
                OFFSET 0 LIMIT {topCount}";

            var iterator = _container.GetItemQueryIterator<dynamic>(query);
            var results = new List<MerchantAnalytics>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    var merchantAnalytics = new MerchantAnalytics(
                        (string)item.merchantName,
                        (MerchantCategory)item.merchantCategory,
                        (long)item.transactionCount,
                        (double)item.totalVolume,
                        (double)item.averageAmount,
                        (long)item.anomalyCount);

                    results.Add(merchantAnalytics);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top merchants analytics");
            throw;
        }
    }

    public async Task<List<MerchantAnalytics>> GetMerchantCategoryAnalyticsAsync()
    {
        await EnsureContainerInitializedAsync();
        if (_container == null)
        {
            _logger.LogError("Container not initialized");
            throw new InvalidOperationException("Container not initialized");
        }

        _logger.LogInformation("Getting merchant category analytics");

        try
        {
            var query = @"
                SELECT 
                    'Category Total' as merchantName,
                    c.merchantCategory,
                    COUNT(1) as transactionCount,
                    SUM(c.amount) as totalVolume,
                    AVG(c.amount) as averageAmount,
                    SUM(CASE WHEN c.anomalyFlag != null THEN 1 ELSE 0 END) as anomalyCount
                FROM c 
                GROUP BY c.merchantCategory 
                ORDER BY SUM(c.amount) DESC";

            var iterator = _container.GetItemQueryIterator<dynamic>(query);
            var results = new List<MerchantAnalytics>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    var merchantAnalytics = new MerchantAnalytics(
                        (string)item.merchantName,
                        (MerchantCategory)item.merchantCategory,
                        (long)item.transactionCount,
                        (double)item.totalVolume,
                        (double)item.averageAmount,
                        (long)item.anomalyCount);

                    results.Add(merchantAnalytics);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merchant category analytics");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing CosmosClient in CosmosDbAnalyticsRepository");
        _cosmosClient?.Dispose();
        await ValueTask.CompletedTask;
    }
}
