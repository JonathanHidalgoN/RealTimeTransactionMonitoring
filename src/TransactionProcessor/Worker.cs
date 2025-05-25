using Confluent.Kafka;
using System.Text.Json;
using FinancialMonitoring.Models;

namespace TransactionProcessor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction Processor Worker starting at: {time}", DateTimeOffset.Now);
            string? kafkaServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
            if (string.IsNullOrEmpty(kafkaServer))
            {
                throw new Exception("No 'KAFKA_BOOTSTRAP_SERVERS' env variable");
            }
            else
            {
                _logger.LogInformation($"KAFKA_BOOTSTRAP_SERVERS info from env: {kafkaServer}");
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

            //Id that comsumer group will consume
            const string topic = "transactions";

            //To leverage disposal connection of Kafka consumer
            using (var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build())
            {
                consumer.Subscribe(topic);
                _logger.LogInformation("Subscribed to topic: {topic}", topic);

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
                                    _logger.LogInformation($"Deserialized Transaction: ID={transaction.TransactionId}, Amount={transaction.Amount}, From={transaction.SourceAccount}");
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
