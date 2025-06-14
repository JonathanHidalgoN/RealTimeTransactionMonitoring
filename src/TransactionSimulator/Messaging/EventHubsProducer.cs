using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;
using System.Text;
using Confluent.Kafka;

namespace TransactionSimulator.Messaging;

public class EventHubsProducer : IMessageProducer<Null, string>
{
    private readonly EventHubProducerClient _producerClient;

    public EventHubsProducer(IOptions<EventHubsSettings> eventHubsSettings)
    {
        _producerClient = new EventHubProducerClient(
            eventHubsSettings.Value.ConnectionString,
            eventHubsSettings.Value.EventHubName);
    }

    public async Task ProduceAsync(Null? key, string value, CancellationToken cancellationToken)
    {
        using EventDataBatch eventBatch = await _producerClient.CreateBatchAsync(cancellationToken);
        if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(value))))
        {
            throw new Exception("Message is too large for the batch.");
        }
        await _producerClient.SendAsync(eventBatch, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.DisposeAsync();
    }
}