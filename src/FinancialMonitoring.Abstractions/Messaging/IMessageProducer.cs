namespace FinancialMonitoring.Abstractions.Messaging;

public interface IMessageProducer<TKey, TValue> : IAsyncDisposable
{
    Task ProduceAsync(TKey? key, TValue value, CancellationToken cancellationToken = default);
}
