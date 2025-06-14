using Confluent.Kafka;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Options;

namespace TransactionSimulator.Messaging;

public class KafkaProducer : IMessageProducer<Null, string>
{
    private readonly IProducer<Null, string> _producer;

    public KafkaProducer(IOptions<KafkaSettings> kafkaSettings)
    {
        var config = new ProducerConfig { BootstrapServers = kafkaSettings.Value.BootstrapServers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task ProduceAsync(Null? key, string value, CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(AppConstants.TransactionsTopicName, 
            new Message<Null, string> { Value = value }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        await ValueTask.CompletedTask;
    }
}