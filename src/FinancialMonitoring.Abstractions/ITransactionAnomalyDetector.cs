using FinancialMonitoring.Models;

namespace FinancialMonitoring.Abstractions.Services;

/// <summary>
/// Defines the contract for a service that detects anomalies in financial transactions.
/// </summary>
public interface ITransactionAnomalyDetector
{
    /// <summary>
    /// Asynchronously detects if a given transaction is anomalous based on implemented rules.
    /// </summary>
    /// <param name="transaction">The transaction to analyze.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a string identifying the type of anomaly (e.g., "HighValueDeviationAnomaly"),
    /// or <c>null</c> if the transaction is not considered anomalous.
    /// </returns>
    Task<string?> DetectAsync(Transaction transaction);
}
