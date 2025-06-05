using Confluent.Kafka;
using System.Text.Json;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions.Services;
using Microsoft.Extensions.Options;

namespace TransactionProcessor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _kafkaBootstrapServers;


        public Worker(ILogger<Worker> logger, ICosmosDbService cosmosDbService, IServiceProvider serviceProvider,
        IOptions<KafkaSettings> kafkaSettingsOptions
        )
        {
            _logger = logger;
            _cosmosDbService = cosmosDbService;
            _serviceProvider = serviceProvider;
            KafkaSettings kafkaSettings = kafkaSettingsOptions.Value;
            _kafkaBootstrapServers = kafkaSettings.BootstrapServers!;
            _logger.LogInformation($"{AppConstants.KafkaConfigPrefix}:{nameof(KafkaSettings.BootstrapServers)} configured via IOptions to: {_kafkaBootstrapServers}");
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction Processor Worker starting at: {time}", DateTimeOffset.Now);

            //Kafka class to config consumer
            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaBootstrapServers,
                //Consumer group 
                GroupId = "transaction-processor-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            //To leverage disposal connection of Kafka consumer()
            using (var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build())
            {
                consumer.Subscribe(AppConstants.TransactionsTopicName);
                _logger.LogInformation("Subscribed to topic: {topic}", AppConstants.TransactionsTopicName);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                            if (consumeResult == null)
                            {
                                await Task.Delay(100, stoppingToken);
                                continue;
                            }

                            _logger.LogInformation($"Consumed message from {consumeResult.TopicPartitionOffset}: {consumeResult.Message.Value}");

                            try
                            {
                                var transaction = JsonSerializer.Deserialize<Transaction>(consumeResult.Message.Value);
                                if (transaction != null)
                                {
                                    _logger.LogInformation($"Deserialized Transaction: ID={transaction.Id}, Amount={transaction.Amount}, From={transaction.SourceAccount}");
                                    using (var scope = _serviceProvider.CreateScope())
                                    {
                                        var anomalyDetector = scope.ServiceProvider.GetRequiredService<ITransactionAnomalyDetector>();
                                        string? anomalyFlag = await anomalyDetector.DetectAsync(transaction);
                                        Transaction processedTransaction = transaction;
                                        TransactionForCosmos cosmosTransaction;
                                        if (!string.IsNullOrEmpty(anomalyFlag))
                                        {
                                            processedTransaction = transaction with { AnomalyFlag = anomalyFlag };
                                            cosmosTransaction = TransactionForCosmos.FromDomainTransaction(processedTransaction);
                                        }
                                        else
                                        {
                                            cosmosTransaction = TransactionForCosmos.FromDomainTransaction(transaction);
                                        }
                                        await _cosmosDbService.AddTransactionAsync(cosmosTransaction);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to deserialize message or message was null: {messageValue}", consumeResult.Message.Value);
                                }
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogError(jsonEx, "Error deserializing message: {messageValue}", consumeResult.Message.Value);
                            }


                            try
                            {
                                consumer.Commit(consumeResult);
                                _logger.LogDebug("Offset committed: {offset}", consumeResult.TopicPartitionOffset);
                            }
                            catch (KafkaException e)
                            {
                                _logger.LogError(e, "Error committing offset: {offset}", consumeResult.TopicPartitionOffset);
                            }

                        }
                        catch (ConsumeException e)
                        {
                            _logger.LogError(e, "Error consuming message: {reason}", e.Error.Reason);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Consume loop canceled.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error in consume loop.");
                            await Task.Delay(5000, stoppingToken);
                        }
                    }
                }
                finally
                {
                    _logger.LogInformation("Closing Kafka consumer.");
                    consumer.Close();
                }
            }
            _logger.LogInformation("Transaction Processor Worker stopping at: {time}", DateTimeOffset.Now);
        }
    }
}
