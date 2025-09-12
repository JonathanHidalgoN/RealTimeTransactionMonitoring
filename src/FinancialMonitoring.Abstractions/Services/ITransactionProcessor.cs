using FinancialMonitoring.Abstractions.Messaging;

namespace FinancialMonitoring.Abstractions.Services;

/// <summary>
/// Defines the contract for processing transaction messages.
/// </summary>
public interface ITransactionProcessor
{
    /// <summary>
    /// Processes a single transaction message from a message broker.
    /// </summary>
    /// <param name="message">The received message containing the transaction data.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task ProcessMessageAsync(ReceivedMessage<object?, string> message);
}
