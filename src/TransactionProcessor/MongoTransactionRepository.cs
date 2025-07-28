using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TransactionProcessor.Services;

public class MongoTransactionRepository : ITransactionRepository, IAsyncDisposable
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Transaction> _collection;
    private readonly MongoDbSettings _settings;
    private readonly ILogger<MongoTransactionRepository> _logger;

    public MongoTransactionRepository(IOptions<MongoDbSettings> mongoDbSettings, ILogger<MongoTransactionRepository> logger)
    {
        _settings = mongoDbSettings.Value;
        _logger = logger;

        _logger.LogInformation("Connecting to MongoDB at {ConnectionString}", _settings.ConnectionString);

        _mongoClient = new MongoClient(_settings.ConnectionString);
        _database = _mongoClient.GetDatabase(_settings.DatabaseName);
        _collection = _database.GetCollection<Transaction>(_settings.CollectionName);
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing MongoDB database '{DatabaseName}' and collection '{CollectionName}'",
                _settings.DatabaseName, _settings.CollectionName);

            // Create index on Id field for faster queries
            var indexKeysDefinition = Builders<Transaction>.IndexKeys.Ascending(t => t.Id);
            var createIndexModel = new CreateIndexModel<Transaction>(indexKeysDefinition, new CreateIndexOptions { Unique = true });
            await _collection.Indexes.CreateOneAsync(createIndexModel);

            // Create index on AnomalyFlag field for anomalous transaction queries
            var anomalyIndexKeysDefinition = Builders<Transaction>.IndexKeys.Ascending(t => t.AnomalyFlag);
            var anomalyCreateIndexModel = new CreateIndexModel<Transaction>(anomalyIndexKeysDefinition);
            await _collection.Indexes.CreateOneAsync(anomalyCreateIndexModel);

            // Create index on Timestamp field for sorting
            var timestampIndexKeysDefinition = Builders<Transaction>.IndexKeys.Descending(t => t.Timestamp);
            var timestampCreateIndexModel = new CreateIndexModel<Transaction>(timestampIndexKeysDefinition);
            await _collection.Indexes.CreateOneAsync(timestampCreateIndexModel);

            _logger.LogInformation("MongoDB initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MongoDB database or collection");
            throw;
        }
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        try
        {
            _logger.LogInformation("Adding transaction with ID '{TransactionId}' to MongoDB", transaction.Id);

            await _collection.ReplaceOneAsync(
                Builders<Transaction>.Filter.Eq(t => t.Id, transaction.Id),
                transaction,
                new ReplaceOptions { IsUpsert = true }
            );

            _logger.LogInformation("Successfully added/updated transaction with ID '{TransactionId}'", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction with ID '{TransactionId}' to MongoDB", transaction.Id);
            throw;
        }
    }

    public async Task<PagedResult<Transaction>?> GetAllTransactionsAsync(int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation("Fetching transactions, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var totalCount = await _collection.CountDocumentsAsync(FilterDefinition<Transaction>.Empty);

            if (totalCount == 0)
            {
                _logger.LogInformation("No transactions found in database");
                return CreateEmptyPagedResult(pageNumber, pageSize);
            }

            var skip = (pageNumber - 1) * pageSize;
            var transactions = await _collection
                .Find(FilterDefinition<Transaction>.Empty)
                .Sort(Builders<Transaction>.Sort.Descending(t => t.Timestamp))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResult<Transaction>
            {
                Items = transactions,
                TotalCount = (int)totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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
            _logger.LogInformation("Fetching transaction by ID: {Id}", id);

            var filter = Builders<Transaction>.Filter.Eq(t => t.Id, id);
            var transaction = await _collection.Find(filter).FirstOrDefaultAsync();

            if (transaction == null)
            {
                _logger.LogWarning("Transaction with ID: {Id} not found", id);
            }

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transaction by ID: {Id}", id);
            return null;
        }
    }

    public async Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize)
    {
        try
        {
            _logger.LogInformation("Fetching anomalous transactions, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var filter = Builders<Transaction>.Filter.Ne(t => t.AnomalyFlag, null);
            var totalCount = await _collection.CountDocumentsAsync(filter);

            if (totalCount == 0)
            {
                _logger.LogInformation("No anomalous transactions found in database");
                return CreateEmptyPagedResult(pageNumber, pageSize);
            }

            var skip = (pageNumber - 1) * pageSize;
            var transactions = await _collection
                .Find(filter)
                .Sort(Builders<Transaction>.Sort.Descending(t => t.Timestamp))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResult<Transaction>
            {
                Items = transactions,
                TotalCount = (int)totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching anomalous transactions, returning empty result");
            return CreateEmptyPagedResult(pageNumber, pageSize);
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

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing MongoDB client");
        await ValueTask.CompletedTask;
    }
}
