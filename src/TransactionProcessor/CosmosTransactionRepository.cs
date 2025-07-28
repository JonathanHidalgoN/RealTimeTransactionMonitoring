using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;

namespace TransactionProcessor.Services;

/// <summary>
/// Cosmos DB implementation of ITransactionRepository for the TransactionProcessor service.
/// This wraps the existing CosmosDbService while providing the unified repository interface.
/// </summary>
public class CosmosTransactionRepository : ITransactionRepository, IAsyncDisposable
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<CosmosTransactionRepository> _logger;

    public CosmosTransactionRepository(
        ICosmosDbService cosmosDbService,
        ILogger<CosmosTransactionRepository> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Cosmos DB through existing service");
        await _cosmosDbService.InitializeDatabaseAsync();
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        _logger.LogInformation("Adding transaction with ID '{TransactionId}' to Cosmos DB", transaction.Id);

        var cosmosTransaction = TransactionForCosmos.FromDomainTransaction(transaction);
        await _cosmosDbService.AddTransactionAsync(cosmosTransaction);
    }

    public Task<PagedResult<Transaction>?> GetAllTransactionsAsync(int pageNumber, int pageSize)
    {
        _logger.LogWarning("GetAllTransactionsAsync not implemented in TransactionProcessor - this service only writes transactions");
        return Task.FromResult<PagedResult<Transaction>?>(new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public Task<Transaction?> GetTransactionByIdAsync(string id)
    {
        _logger.LogWarning("GetTransactionByIdAsync not implemented in TransactionProcessor - this service only writes transactions");
        return Task.FromResult<Transaction?>(null);
    }

    public Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize)
    {
        _logger.LogWarning("GetAnomalousTransactionsAsync not implemented in TransactionProcessor - this service only writes transactions");
        return Task.FromResult<PagedResult<Transaction>?>(new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing Cosmos DB repository");

        if (_cosmosDbService is IAsyncDisposable cosmosDisposable)
            await cosmosDisposable.DisposeAsync();
    }
}
