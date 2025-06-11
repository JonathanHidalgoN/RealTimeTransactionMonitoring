namespace FinancialMonitoring.Abstractions.Messaging;

public record ReceivedMessage<TKey, TValue>(TKey Key, TValue Value);

public interface IMessageConsumer<TKey, TValue> : IAsyncDisposable
{
    Task ConsumeAsync(Func<ReceivedMessage<TKey, TValue>, Task> messageHandler, CancellationToken cancellationToken);
}