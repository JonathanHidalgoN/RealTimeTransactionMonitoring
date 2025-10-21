using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace FinancialMonitoring.Abstractions.Persistence;

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

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing MongoDB database '{DatabaseName}' and collection '{CollectionName}'",
                _settings.DatabaseName, _settings.CollectionName);


            await CreateIndexIfNotExistsAsync(
                "idx_anomaly_flag",
                Builders<Transaction>.IndexKeys.Ascending(t => t.AnomalyFlag),
                new CreateIndexOptions { Background = true },
                cancellationToken);

            await CreateIndexIfNotExistsAsync(
                "idx_timestamp",
                Builders<Transaction>.IndexKeys.Descending(t => t.Timestamp),
                new CreateIndexOptions { Background = true },
                cancellationToken);

            _logger.LogInformation("MongoDB initialization completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MongoDB initialization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MongoDB database or collection");
            throw;
        }
    }

    private async Task CreateIndexIfNotExistsAsync(string indexName, IndexKeysDefinition<Transaction> indexKeys, CreateIndexOptions options, CancellationToken cancellationToken)
    {
        try
        {
            if (await IndexExistsAsync(indexName, cancellationToken))
            {
                _logger.LogInformation("Index '{IndexName}' already exists, skipping creation", indexName);
                return;
            }

            options.Name = indexName;

            var createIndexModel = new CreateIndexModel<Transaction>(indexKeys, options);
            await _collection.Indexes.CreateOneAsync(createIndexModel, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully created index '{IndexName}'", indexName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict")
        {
            _logger.LogWarning("Index '{IndexName}' already exists with different options: {ErrorMessage}", indexName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create index '{IndexName}'", indexName);
            throw;
        }
    }

    private async Task<bool> IndexExistsAsync(string indexName, CancellationToken cancellationToken)
    {
        try
        {
            using var cursor = await _collection.Indexes.ListAsync(cancellationToken);
            var indexes = await cursor.ToListAsync(cancellationToken);

            return indexes.Any(index =>
                index.TryGetValue("name", out var nameValue) &&
                nameValue.AsString == indexName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if index '{IndexName}' exists, assuming it doesn't", indexName);
            return false;
        }
    }

    public async Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Adding transaction with ID '{TransactionId}' to MongoDB", transaction.Id);

            await _collection.ReplaceOneAsync(
                Builders<Transaction>.Filter.Eq(t => t.Id, transaction.Id),
                transaction,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken: cancellationToken
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

    public async Task<PagedResult<Transaction>?> SearchTransactionsAsync(TransactionSearchRequest searchRequest)
    {
        try
        {
            _logger.LogInformation("Searching transactions with advanced criteria, Page: {PageNumber}, Size: {PageSize}",
                searchRequest.PageNumber, searchRequest.PageSize);

            var filterBuilder = Builders<Transaction>.Filter;
            var filters = new List<FilterDefinition<Transaction>>();

            // Time range filter
            if (searchRequest.FromTimestamp.HasValue)
            {
                filters.Add(filterBuilder.Gte(t => t.Timestamp, searchRequest.FromTimestamp.Value));
            }
            if (searchRequest.ToTimestamp.HasValue)
            {
                filters.Add(filterBuilder.Lte(t => t.Timestamp, searchRequest.ToTimestamp.Value));
            }

            // Amount range filter
            if (searchRequest.MinAmount.HasValue)
            {
                filters.Add(filterBuilder.Gte(t => t.Amount, searchRequest.MinAmount.Value));
            }
            if (searchRequest.MaxAmount.HasValue)
            {
                filters.Add(filterBuilder.Lte(t => t.Amount, searchRequest.MaxAmount.Value));
            }

            // Merchant filters
            if (searchRequest.MerchantCategory.HasValue)
            {
                filters.Add(filterBuilder.Eq(t => t.MerchantCategory, searchRequest.MerchantCategory.Value));
            }
            if (!string.IsNullOrWhiteSpace(searchRequest.MerchantName))
            {
                filters.Add(filterBuilder.Regex(t => t.MerchantName, new MongoDB.Bson.BsonRegularExpression(searchRequest.MerchantName, "i")));
            }

            // Payment method filter
            if (searchRequest.PaymentMethod.HasValue)
            {
                filters.Add(filterBuilder.Eq(t => t.PaymentMethod, searchRequest.PaymentMethod.Value));
            }

            // Account filters
            if (!string.IsNullOrWhiteSpace(searchRequest.SourceAccountId))
            {
                filters.Add(filterBuilder.Eq(t => t.SourceAccount.AccountId, searchRequest.SourceAccountId));
            }
            if (!string.IsNullOrWhiteSpace(searchRequest.DestinationAccountId))
            {
                filters.Add(filterBuilder.Eq(t => t.DestinationAccount.AccountId, searchRequest.DestinationAccountId));
            }

            // Transaction type filter
            if (searchRequest.TransactionType.HasValue)
            {
                filters.Add(filterBuilder.Eq(t => t.Type, searchRequest.TransactionType.Value));
            }

            // Anomaly filters
            if (searchRequest.AnomaliesOnly)
            {
                filters.Add(filterBuilder.Ne(t => t.AnomalyFlag, null));
            }
            else if (!string.IsNullOrWhiteSpace(searchRequest.AnomalyFlag))
            {
                filters.Add(filterBuilder.Eq(t => t.AnomalyFlag, searchRequest.AnomalyFlag));
            }

            // Location filters
            if (!string.IsNullOrWhiteSpace(searchRequest.City))
            {
                filters.Add(filterBuilder.Eq(t => t.Location.City, searchRequest.City));
            }
            if (!string.IsNullOrWhiteSpace(searchRequest.State))
            {
                filters.Add(filterBuilder.Eq(t => t.Location.State, searchRequest.State));
            }

            // Combine all filters
            var finalFilter = filters.Count > 0 ? filterBuilder.And(filters) : FilterDefinition<Transaction>.Empty;

            var totalCount = await _collection.CountDocumentsAsync(finalFilter);

            if (totalCount == 0)
            {
                _logger.LogInformation("No transactions found matching search criteria");
                return CreateEmptyPagedResult(searchRequest.PageNumber, searchRequest.PageSize);
            }

            // Build sort definition
            var sortBuilder = Builders<Transaction>.Sort;
            SortDefinition<Transaction> sort = searchRequest.SortBy?.ToLower() switch
            {
                "amount" => searchRequest.SortDirection?.ToLower() == "asc"
                    ? sortBuilder.Ascending(t => t.Amount)
                    : sortBuilder.Descending(t => t.Amount),
                "timestamp" => searchRequest.SortDirection?.ToLower() == "asc"
                    ? sortBuilder.Ascending(t => t.Timestamp)
                    : sortBuilder.Descending(t => t.Timestamp),
                _ => sortBuilder.Descending(t => t.Timestamp) // Default sort
            };

            var skip = (searchRequest.PageNumber - 1) * searchRequest.PageSize;
            var transactions = await _collection
                .Find(finalFilter)
                .Sort(sort)
                .Skip(skip)
                .Limit(searchRequest.PageSize)
                .ToListAsync();

            return new PagedResult<Transaction>
            {
                Items = transactions,
                TotalCount = (int)totalCount,
                PageNumber = searchRequest.PageNumber,
                PageSize = searchRequest.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transactions, returning empty result");
            return CreateEmptyPagedResult(searchRequest.PageNumber, searchRequest.PageSize);
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
        await ValueTask.CompletedTask; // MongoDB client doesn't need explicit disposal
    }
}
