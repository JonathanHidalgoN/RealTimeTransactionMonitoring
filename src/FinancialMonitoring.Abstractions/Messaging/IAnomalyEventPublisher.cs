using FinancialMonitoring.Models;
using System.Threading.Tasks;

namespace FinancialMonitoring.Abstractions.Messaging;

/// <summary>
/// Defines the contract for a service that publishes information about anomalous transactions.
/// </summary>
public interface IAnomalyEventPublisher
{
    /// <summary>
    /// Publishes an event indicating that a transaction has been identified as anomalous.
    /// </summary>
    /// <param name="anomalousTransaction">The transaction that was flagged as an anomaly.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous publishing operation.</returns>
    Task PublishAsync(Transaction anomalousTransaction);
}
