using Confluent.Kafka;
using System.Text.Json;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;

namespace TransactionProcessor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, ICosmosDbService cosmosDbService)
        {
            _logger = logger;
            _configuration = configuration;
            _cosmosDbService = cosmosDbService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction Processor Worker starting at: {time}", DateTimeOffset.Now);
            string? kafkaServer = Environment.GetEnvironmentVariable(AppConstants.KafkaBootstrapServersEnvVarName);
            if (string.IsNullOrEmpty(kafkaServer))
            {
                throw new Exception($"No '{AppConstants.KafkaBootstrapServersEnvVarName}' env variable");
            }
            else
            {
                _logger.LogInformation($"{AppConstants.KafkaBootstrapServersEnvVarName} info from env: {kafkaServer}");
            }

            //Kafka class to config consumer
            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                BootstrapServers = kafkaServer,
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
                                    TransactionForCosmos cosmosTransaction = TransactionForCosmos.FromDomainTransaction(transaction);
                                    await _cosmosDbService.AddTransactionAsync(cosmosTransaction);
                                    // TODO: Add processing logic here (e.g., anomaly detection)
                                    // TODO: Add persistence logic here (e.g., save to database)
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
