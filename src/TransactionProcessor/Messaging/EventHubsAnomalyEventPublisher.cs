using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace TransactionProcessor.Messaging;

public class EventHubsAnomalyEventPublisher : IAnomalyEventPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;

    public EventHubsAnomalyEventPublisher(IOptions<EventHubsSettings> settings)
    {
        var eventHubsSettings = settings.Value;
        _producerClient = new EventHubProducerClient(
            eventHubsSettings.ConnectionString,
            AppConstants.AnomaliesTopicName);
    }

    public async Task PublishAsync(Transaction anomalousTransaction)
    {
        using EventDataBatch eventBatch = await _producerClient.CreateBatchAsync();
        var messageBody = JsonSerializer.Serialize(anomalousTransaction);
        if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(messageBody))))
        {
            throw new Exception($"Event for transaction {anomalousTransaction.Id} is too large for the batch and cannot be sent.");
        }
        await _producerClient.SendAsync(eventBatch);
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.DisposeAsync();
    }
}
