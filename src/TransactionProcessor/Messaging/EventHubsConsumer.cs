using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Confluent.Kafka;

namespace TransactionProcessor.Messaging;

public class EventHubsConsumer : IMessageConsumer<Null, string>
{
    private readonly ILogger<EventHubsConsumer> _logger;
    private readonly EventProcessorClient _processorClient;
    private Func<ReceivedMessage<Null, string>, Task> _messageHandler = _ => Task.CompletedTask;

    public EventHubsConsumer(IOptions<EventHubsSettings> settings, ILogger<EventHubsConsumer> logger)
    {
        _logger = logger;
        var eventHubsSettings = settings.Value;

        var storageClient = new BlobContainerClient(
            eventHubsSettings.BlobStorageConnectionString,
            eventHubsSettings.BlobContainerName);

        _processorClient = new EventProcessorClient(
            storageClient,
            EventHubConsumerClient.DefaultConsumerGroupName,
            eventHubsSettings.ConnectionString,
            eventHubsSettings.EventHubName);

        _processorClient.ProcessEventAsync += ProcessEventHandler;
        _processorClient.ProcessErrorAsync += ProcessErrorHandler;
    }

    public async Task ConsumeAsync(Func<ReceivedMessage<Null, string>, Task> messageHandler, CancellationToken cancellationToken)
    {
        _messageHandler = messageHandler;
        _logger.LogInformation("EventHubsConsumer starting message processing...");
        await _processorClient.StartProcessingAsync(cancellationToken);

        // Keep this method running until cancellation is requested
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        try
        {
            var messageValue = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
            var receivedMessage = new ReceivedMessage<Null, string>(null, messageValue);
            await _messageHandler(receivedMessage);
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event from partition {PartitionId}", eventArgs.Partition.PartitionId);
        }
    }

    private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
    {
        _logger.LogError(eventArgs.Exception, "Error in EventHubsConsumer partition {PartitionId}, Operation: {Operation}",
            eventArgs.PartitionId, eventArgs.Operation);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("EventHubsConsumer stopping message processing...");
        await _processorClient.StopProcessingAsync();
        _processorClient.ProcessEventAsync -= ProcessEventHandler;
        _processorClient.ProcessErrorAsync -= ProcessErrorHandler;
    }
}
