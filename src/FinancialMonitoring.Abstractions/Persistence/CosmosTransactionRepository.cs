using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;

namespace FinancialMonitoring.Abstractions.Persistence;

/// <summary>
/// Cosmos DB implementation of ITransactionRepository that wraps existing Cosmos services.
/// </summary>
public class CosmosTransactionRepository : ITransactionRepository
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ITransactionQueryService? _queryService;
    private readonly ILogger<CosmosTransactionRepository> _logger;
    private readonly bool _hasQueryService;

    public CosmosTransactionRepository(
        ICosmosDbService cosmosDbService,
        ILogger<CosmosTransactionRepository> logger,
        ITransactionQueryService? queryService = null)
    {
        _cosmosDbService = cosmosDbService;
        _queryService = queryService;
        _logger = logger;
        _hasQueryService = queryService != null;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Cosmos DB through existing service");
        await _cosmosDbService.InitializeDatabaseAsync(cancellationToken);
    }

    public async Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding transaction with ID '{TransactionId}' to Cosmos DB", transaction.Id);

        // Convert domain Transaction to TransactionForCosmos (preserving the ID mapping)
        var cosmosTransaction = TransactionForCosmos.FromDomainTransaction(transaction);
        await _cosmosDbService.AddTransactionAsync(cosmosTransaction, cancellationToken);
    }

    public async Task<PagedResult<Transaction>?> GetAllTransactionsAsync(int pageNumber, int pageSize)
    {
        if (!_hasQueryService)
        {
            _logger.LogWarning("GetAllTransactionsAsync not available - no query service configured");
            return CreateEmptyPagedResult(pageNumber, pageSize);
        }

        _logger.LogInformation("Fetching all transactions from Cosmos DB, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        return await _queryService!.GetAllTransactionsAsync(pageNumber, pageSize);
    }

    public async Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        if (!_hasQueryService)
        {
            _logger.LogWarning("GetTransactionByIdAsync not available - no query service configured");
            return null;
        }

        _logger.LogInformation("Fetching transaction by ID: {Id} from Cosmos DB", id);
        return await _queryService!.GetTransactionByIdAsync(id);
    }

    public async Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize)
    {
        if (!_hasQueryService)
        {
            _logger.LogWarning("GetAnomalousTransactionsAsync not available - no query service configured");
            return CreateEmptyPagedResult(pageNumber, pageSize);
        }

        _logger.LogInformation("Fetching anomalous transactions from Cosmos DB, Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        return await _queryService!.GetAnomalousTransactionsAsync(pageNumber, pageSize);
    }

    public async Task<PagedResult<Transaction>?> SearchTransactionsAsync(TransactionSearchRequest searchRequest)
    {
        if (!_hasQueryService)
        {
            _logger.LogWarning("SearchTransactionsAsync not available - no query service configured");
            return CreateEmptyPagedResult(searchRequest.PageNumber, searchRequest.PageSize);
        }

        _logger.LogInformation("Searching transactions in Cosmos DB with advanced criteria, Page: {PageNumber}, Size: {PageSize}",
            searchRequest.PageNumber, searchRequest.PageSize);

        if (searchRequest.AnomaliesOnly)
        {
            return await _queryService!.GetAnomalousTransactionsAsync(searchRequest.PageNumber, searchRequest.PageSize);
        }

        return await _queryService!.GetAllTransactionsAsync(searchRequest.PageNumber, searchRequest.PageSize);
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
}
