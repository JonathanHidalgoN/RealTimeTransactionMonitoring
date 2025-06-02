using Confluent.Kafka;
using System.Text.Json;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace TransactionSimulator;


public class Simulator : BackgroundService
{
    private readonly ILogger<Simulator> _logger;
    private readonly string _kafkaBootstrapServers;
    private static readonly Random _random = new Random();

    public Simulator(ILogger<Simulator> logger, IConfiguration configuration)
    {
        _logger = logger;
        _kafkaBootstrapServers = configuration[AppConstants.KafkaBootstrapServersEnvVarName]
                                ?? configuration[$"Values:{AppConstants.KafkaBootstrapServersEnvVarName}"]
                                ?? "localhost:9092";

        _logger.LogInformation($"{AppConstants.KafkaBootstrapServersEnvVarName} configured to: {_kafkaBootstrapServers}");
        if (_kafkaBootstrapServers == "localhost:9092")
        {
            _logger.LogWarning("Using fallback Kafka bootstrap servers: localhost:9092");
        }
    }

    private static Transaction GenerateTransaction()
    {
        string sourceAccId = "ACC" + _random.Next(1000, 9999);
        string destAccId = "ACC" + _random.Next(1000, 9999);

        return new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: Math.Round(_random.NextDouble() * 1000, 2),
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sourceAccount: new Account(sourceAccId),
            destinationAccount: new Account(destAccId)
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) // Implement ExecuteAsync
    {
        _logger.LogInformation("Transaction Simulator engine starting (as BackgroundService)...");
        
        var producerConfig = new ProducerConfig { BootstrapServers = _kafkaBootstrapServers };
        int transactionCounter = 0;

        using (var producer = new ProducerBuilder<Null, string>(producerConfig).Build())
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                transactionCounter++;
                Transaction transaction = GenerateTransaction();
                string jsonTransaction = JsonSerializer.Serialize(transaction);

                try
                {
                    var deliveryResult = await producer.ProduceAsync(
                        AppConstants.TransactionsTopicName,
                        new Message<Null, string> { Value = jsonTransaction },
                        stoppingToken); // Pass stoppingToken

                    _logger.LogInformation("[{Timestamp:HH:mm:ss}] Produced message {Counter} to {TopicPartitionOffset}: {JsonTransaction}",
                        DateTime.Now, transactionCounter, deliveryResult.TopicPartitionOffset, jsonTransaction);
                }
                catch (ProduceException<Null, string> e)
                {
                    _logger.LogError(e, "Failed to deliver message: {Reason}", e.Error.Reason);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Message production was canceled.");
                    break; 
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Simulation delay canceled. Exiting loop.");
                    break;
                }
            }
            _logger.LogInformation("Flushing producer...");
            producer.Flush(TimeSpan.FromSeconds(10));
        }
        _logger.LogInformation("Transaction Simulator engine stopped.");
    }
}