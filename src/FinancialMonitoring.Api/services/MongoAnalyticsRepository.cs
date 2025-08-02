using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using MongoDB.Driver;
using MongoDB.Bson;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// MongoDB implementation of the analytics repository for development/testing.
/// </summary>
public class MongoAnalyticsRepository : IAnalyticsRepository
{
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly ILogger<MongoAnalyticsRepository> _logger;

    public MongoAnalyticsRepository(IMongoDatabase database, ILogger<MongoAnalyticsRepository> logger)
    {
        _transactions = database.GetCollection<Transaction>("transactions");
        _logger = logger;
    }

    public async Task<TransactionAnalytics> GetTransactionAnalyticsAsync()
    {
        _logger.LogInformation("Calculating global transaction analytics");

        try
        {
            var totalTransactions = await _transactions.CountDocumentsAsync(FilterDefinition<Transaction>.Empty);
            var totalAnomalies = await _transactions.CountDocumentsAsync(Builders<Transaction>.Filter.Ne(x => x.AnomalyFlag, null));

            // Calculate volume and average using aggregation
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "totalVolume", new BsonDocument("$sum", "$amount") },
                    { "averageAmount", new BsonDocument("$avg", "$amount") },
                    { "uniqueAccounts", new BsonDocument("$addToSet", "$sourceAccount.accountId") }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "totalVolume", 1 },
                    { "averageAmount", 1 },
                    { "uniqueAccounts", new BsonDocument("$size", "$uniqueAccounts") }
                })
            };

            var aggregationResult = await _transactions.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            
            var totalVolume = aggregationResult?["totalVolume"]?.AsDouble ?? 0.0;
            var averageAmount = aggregationResult?["averageAmount"]?.AsDouble ?? 0.0;
            var uniqueAccounts = aggregationResult?["uniqueAccounts"]?.AsInt64 ?? 0;

            // Calculate last 24 hours statistics
            var last24Hours = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();
            var recentFilter = Builders<Transaction>.Filter.Gte(x => x.Timestamp, last24Hours);
            
            var transactionsLast24Hours = await _transactions.CountDocumentsAsync(recentFilter);
            var anomaliesLast24Hours = await _transactions.CountDocumentsAsync(
                Builders<Transaction>.Filter.And(recentFilter, Builders<Transaction>.Filter.Ne(x => x.AnomalyFlag, null)));

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
        _logger.LogInformation("Getting transaction time series from {FromTimestamp} to {ToTimestamp} with {IntervalMinutes} minute intervals", 
            fromTimestamp, toTimestamp, intervalMinutes);

        try
        {
            var intervalMs = intervalMinutes * 60 * 1000;
            var dataPoints = new List<TimeSeriesDataPoint>();

            for (var currentTime = fromTimestamp; currentTime < toTimestamp; currentTime += intervalMs)
            {
                var nextTime = Math.Min(currentTime + intervalMs, toTimestamp);
                
                var filter = Builders<Transaction>.Filter.And(
                    Builders<Transaction>.Filter.Gte(x => x.Timestamp, currentTime),
                    Builders<Transaction>.Filter.Lt(x => x.Timestamp, nextTime));

                var count = await _transactions.CountDocumentsAsync(filter);
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
        _logger.LogInformation("Getting anomaly time series from {FromTimestamp} to {ToTimestamp} with {IntervalMinutes} minute intervals", 
            fromTimestamp, toTimestamp, intervalMinutes);

        try
        {
            var intervalMs = intervalMinutes * 60 * 1000;
            var dataPoints = new List<TimeSeriesDataPoint>();

            for (var currentTime = fromTimestamp; currentTime < toTimestamp; currentTime += intervalMs)
            {
                var nextTime = Math.Min(currentTime + intervalMs, toTimestamp);
                
                var filter = Builders<Transaction>.Filter.And(
                    Builders<Transaction>.Filter.Gte(x => x.Timestamp, currentTime),
                    Builders<Transaction>.Filter.Lt(x => x.Timestamp, nextTime),
                    Builders<Transaction>.Filter.Ne(x => x.AnomalyFlag, null));

                var count = await _transactions.CountDocumentsAsync(filter);
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
        _logger.LogInformation("Getting top {TopCount} merchants analytics", topCount);

        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument { { "merchantName", "$merchantName" }, { "merchantCategory", "$merchantCategory" } } },
                    { "transactionCount", new BsonDocument("$sum", 1) },
                    { "totalVolume", new BsonDocument("$sum", "$amount") },
                    { "averageAmount", new BsonDocument("$avg", "$amount") },
                    { "anomalyCount", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { new BsonDocument("$ne", new BsonArray { "$anomalyFlag", BsonNull.Value }), 1, 0 })) }
                }),
                new BsonDocument("$sort", new BsonDocument("totalVolume", -1)),
                new BsonDocument("$limit", topCount)
            };

            var results = await _transactions.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            return results.Select(doc => new MerchantAnalytics(
                doc["_id"]["merchantName"].AsString,
                (MerchantCategory)doc["_id"]["merchantCategory"].AsInt32,
                doc["transactionCount"].AsInt64,
                doc["totalVolume"].AsDouble,
                doc["averageAmount"].AsDouble,
                doc["anomalyCount"].AsInt64
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top merchants analytics");
            throw;
        }
    }

    public async Task<List<MerchantAnalytics>> GetMerchantCategoryAnalyticsAsync()
    {
        _logger.LogInformation("Getting merchant category analytics");

        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$merchantCategory" },
                    { "transactionCount", new BsonDocument("$sum", 1) },
                    { "totalVolume", new BsonDocument("$sum", "$amount") },
                    { "averageAmount", new BsonDocument("$avg", "$amount") },
                    { "anomalyCount", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { new BsonDocument("$ne", new BsonArray { "$anomalyFlag", BsonNull.Value }), 1, 0 })) }
                }),
                new BsonDocument("$sort", new BsonDocument("totalVolume", -1))
            };

            var results = await _transactions.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            return results.Select(doc => new MerchantAnalytics(
                "Category Total",
                (MerchantCategory)doc["_id"].AsInt32,
                doc["transactionCount"].AsInt64,
                doc["totalVolume"].AsDouble,
                doc["averageAmount"].AsDouble,
                doc["anomalyCount"].AsInt64
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merchant category analytics");
            throw;
        }
    }
}