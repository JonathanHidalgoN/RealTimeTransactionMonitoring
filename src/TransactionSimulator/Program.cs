using Confluent.Kafka;
using System.Text.Json;
using FinancialMonitoring.Models;

bool running = true;

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Exit requested...");
    running = false;
    eventArgs.Cancel = true;
};

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");


if (string.IsNullOrEmpty(kafkaBootstrapServers))
{
    throw new Exception("No 'KAFKA_BOOTSTRAP_SERVERS' env variable");
}
else
{
    Console.WriteLine($"KAFKA_BOOTSTRAP_SERVERS info from env: {kafkaBootstrapServers}");
}

ProducerConfig producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers
};

const string topic = "transactions";
int transactionCounter = 0;

using (var producer = new ProducerBuilder<Null, string>(producerConfig).Build())
{
    while (running)
    {
        transactionCounter++;
        string sourceAccId = "ACC" + new Random().Next(1000, 9999);
        string destAccId = "ACC" + new Random().Next(1000, 9999);

        Transaction transaction = new Transaction(
            transactionId: Guid.NewGuid().ToString(),
            amount: Math.Round(new Random().NextDouble() * 1000, 2),
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sourceAccount: new Account(sourceAccId),
            destinationAccount: new Account(destAccId)
        );

        string jsonTransaction = JsonSerializer.Serialize(transaction);

        try
        {
            var deliveryResult = await producer.ProduceAsync(topic, new Message<Null, string> { Value = jsonTransaction });
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Produced message {transactionCounter} to {deliveryResult.TopicPartitionOffset}: {jsonTransaction}");
        }
        catch (ProduceException<Null, string> e)
        {
            Console.WriteLine($"Failed to deliver message: {e.Message} [Reason: {e.Error.Reason}]");
        }

        try
        {
            // Wait before sending the next message
            await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
        }
        catch (TaskCanceledException) { running = false; }
    }
    producer.Flush(TimeSpan.FromSeconds(10));
}
Console.WriteLine("Transaction Simulator shutting down.");
