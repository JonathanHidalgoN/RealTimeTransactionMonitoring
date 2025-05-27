using Confluent.Kafka;
using System.Text.Json;
using FinancialMonitoring.Models;

namespace TransactionSimulator
{
    public class Simulator
    {
        private readonly string _kafkaServers;
        private readonly IProducer<Null, string> _producer;
        private static readonly Random _random = new Random();

        public Simulator(string kafkaServer)
        {
            if (string.IsNullOrWhiteSpace(kafkaServer))
            {
                throw new ArgumentException("Kafka bootstrap servers cannot be null or empty.", nameof(kafkaServer));
            }
            _kafkaServers = kafkaServer;
            Console.WriteLine($"{AppConstants.KafkaBootstrapServersEnvVarName} configured to: {_kafkaServers}");

            var producerConfig = new ProducerConfig { BootstrapServers = _kafkaServers };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        private static Transaction GenerateTransaction()
        {
            string sourceAccId = "ACC" + _random.Next(1000, 9999);
            string destAccId = "ACC" + _random.Next(1000, 9999);

            return new Transaction(
                transactionId: Guid.NewGuid().ToString(),
                amount: Math.Round(_random.NextDouble() * 1000, 2),
                timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sourceAccount: new Account(sourceAccId),
                destinationAccount: new Account(destAccId)
            );
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Transaction Simulator engine starting...");
            int transactionCounter = 0;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    transactionCounter++;
                    Transaction transaction = GenerateTransaction();
                    string jsonTransaction = JsonSerializer.Serialize(transaction);

                    try
                    {
                        var deliveryResult = await _producer.ProduceAsync(
                            AppConstants.TransactionsTopicName,
                            new Message<Null, string> { Value = jsonTransaction },
                            cancellationToken);

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Produced message {transactionCounter} to {deliveryResult.TopicPartitionOffset}: {jsonTransaction}");
                    }
                    catch (ProduceException<Null, string> e)
                    {
                        Console.WriteLine($"Failed to deliver message: {e.Message} [Reason: {e.Error.Reason}]");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Message production was canceled.");
                        break;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Simulation loop was canceled.");
            }
            finally
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
                _producer.Dispose();
                Console.WriteLine("Transaction Simulator engine stopped.");
            }
        }

    }
}
