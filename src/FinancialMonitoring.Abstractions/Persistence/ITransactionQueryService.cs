using FinancialMonitoring.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialMonitoring.Abstractions.Persistence
{
    /// <summary>
    /// Defines the contract for a service that queries transaction data.
    /// </summary>
    public interface ITransactionQueryService
    {
        /// <summary>
        /// Retrieves a paginated list of all transactions.
        /// </summary>
        /// <param name="pageNumber">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A <see cref="PagedResult{T}"/> containing the transactions for the specified page.</returns>
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
        /// <returns>A <see cref="PagedResult{T}"/> containing the anomalous transactions for the specified page.</returns>
        Task<PagedResult<Transaction>?> GetAnomalousTransactionsAsync(int pageNumber, int pageSize);
    }
}
