using FinancialMonitoring.Models;

namespace FinancialMonitoring.Abstractions.Persistence;

/// <summary>
/// Defines the contract for a service that interacts with the Cosmos DB data store.
/// </summary>
public interface ICosmosDbService
{
    /// <summary>
    /// Adds a new transaction document to the database.
    /// </summary>
    /// <param name="item">The transaction object to store.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddTransactionAsync(TransactionForCosmos item);

    /// <summary>
    /// Ensures that the database and its container are created if they do not already exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous initialization operation.</returns>
    Task InitializeDatabaseAsync();
}
