using FinancialMonitoring.Models;

namespace FinancialMonitoring.Abstractions.Persistence;

/// <summary>
/// This interface abstracts the underlying data store implementation
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Initializes the database and ensures required collections/containers exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous initialization operation.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Adds a new transaction to the repository.
    /// </summary>
    /// <param name="transaction">The transaction to store.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddTransactionAsync(Transaction transaction);

    /// <summary>
    /// Retrieves a paginated list of all transactions.
    /// </summary>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PagedResult{Transaction}"/> containing the transactions for the specified page.</returns>
    Task<PagedResult<Transaction>?> GetAllTransactionsAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Retrieves a single transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction.</param>
    /// <returns>The <see cref="Transaction"/> if found; otherwise, <c>null</c>.</returns>
    Task<Transaction?> GetTransactionByIdAsync(string id);

    /// <summary>
    /// Retrieves a paginated list of transactions that have been flagged as anomalous.
    /// </summary>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PagedResult{Transaction}"/> containing the anomalous transactions for the specified page.</returns>
    Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize);

    /// <summary>
    /// Searches for transactions using advanced filtering criteria.
    /// </summary>
    /// <param name="searchRequest">The search criteria and pagination parameters.</param>
    /// <returns>A <see cref="PagedResult{Transaction}"/> containing the matching transactions.</returns>
    Task<PagedResult<Transaction>?> SearchTransactionsAsync(TransactionSearchRequest searchRequest);
}
