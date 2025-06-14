using Confluent.Kafka;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;

namespace TransactionProcessor.Messaging;

public class KafkaConsumer : IMessageConsumer<Null, string>
{
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IConsumer<Ignore, string> _consumer;

    public KafkaConsumer(IOptions<KafkaSettings> kafkaSettings, ILogger<KafkaConsumer> logger)
    {
        _logger = logger;
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.Value.BootstrapServers,
            GroupId = kafkaSettings.Value.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    }

    public async Task ConsumeAsync(Func<ReceivedMessage<Null, string>, Task> messageHandler, CancellationToken cancellationToken)
    {
        _consumer.Subscribe(AppConstants.TransactionsTopicName);
        _logger.LogInformation("KafkaConsumer subscribed to topic: {Topic}", AppConstants.TransactionsTopicName);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                if (consumeResult?.Message != null)
                {
                    var receivedMessage = new ReceivedMessage<Null, string>(null, consumeResult.Message.Value);
                    await messageHandler(receivedMessage);
                    _consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumption loop was cancelled.");
        }
        finally
        {
            _consumer.Close();
        }
    }

    public ValueTask DisposeAsync()
    {
        _consumer.Dispose();
        return ValueTask.CompletedTask;
    }
}
