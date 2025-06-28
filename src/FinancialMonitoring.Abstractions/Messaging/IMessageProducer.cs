namespace FinancialMonitoring.Abstractions.Messaging;

/// <summary>
/// Defines the contract for a generic message producer.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
public interface IMessageProducer<TKey, TValue> : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously produces and sends a message to a message broker.
    /// </summary>
    /// <param name="key">The message key, used for partitioning or ordering. Can be null.</param>
    /// <param name="value">The message payload to send.</param>
    /// <param name="cancellationToken">A token to signal cancellation of the produce operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ProduceAsync(TKey? key, TValue value, CancellationToken cancellationToken = default);
}
