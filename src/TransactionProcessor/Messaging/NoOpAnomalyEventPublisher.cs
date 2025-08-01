using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;

namespace TransactionProcessor.Messaging;

/// <summary>
/// A no-operation implementation of IAnomalyEventPublisher for local development.
/// This class satisfies dependency injection requirements without requiring Azure Event Hubs.
/// </summary>
public class NoOpAnomalyEventPublisher : IAnomalyEventPublisher
{
    public Task PublishAsync(Transaction transaction)
    {
        // No-op: Do nothing for local development
        return Task.CompletedTask;
    }
}