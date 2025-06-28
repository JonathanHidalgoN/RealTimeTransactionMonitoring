namespace FinancialMonitoring.Abstractions.Messaging;

/// <summary>
/// Represents a message received from a message broker.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
/// <param name="Key">The message key, which can be null.</param>
/// <param name="Value">The message payload.</param>
public record ReceivedMessage<TKey, TValue>(TKey? Key, TValue Value);

/// <summary>
/// Defines the contract for a generic message consumer.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
public interface IMessageConsumer<TKey, TValue> : IAsyncDisposable
{
    /// <summary>
    /// Starts consuming messages from the configured source and processes them using the provided handler.
    /// </summary>
    /// <param name="messageHandler">A function that processes a single <see cref="ReceivedMessage{TKey, TValue}"/>.</param>
    /// <param name="cancellationToken">A token to signal that consumption should be stopped.</param>
    /// <returns>A <see cref="Task"/> that completes when the consumption loop is stopped.</returns>
    Task ConsumeAsync(Func<ReceivedMessage<TKey, TValue>, Task> messageHandler, CancellationToken cancellationToken);
}
