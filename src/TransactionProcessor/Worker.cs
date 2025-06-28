using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using System.Text.Json;
using Confluent.Kafka;
using FinancialMonitoring.Abstractions.Persistence;

namespace TransactionProcessor;

/// <summary>
/// The main background service responsible for processing financial transactions.
/// </summary>
/// <remarks>
/// This worker consumes messages from a message broker, deserializes them, detects anomalies,
/// and persists the results to a data store. It runs as a long-running background task.
/// </remarks>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageConsumer<Null, string> _messageConsumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording operational information.</param>
    /// <param name="serviceProvider">The service provider to create dependency scopes.</param>
    /// <param name="messageConsumer">The message consumer to receive transactions from.</param>
    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IMessageConsumer<Null, string> messageConsumer)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageConsumer = messageConsumer;
    }

    /// <summary>
    /// Executes the main logic of the worker, which is to start the message consumption loop.
    /// </summary>
    /// <param name="stoppingToken">A token that signals when the worker should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting consumption loop.");
        await _messageConsumer.ConsumeAsync(ProcessMessageAsync, stoppingToken);
        _logger.LogInformation("Worker consumption loop finished.");
    }

    /// <summary>
    /// Processes a single message received from the message broker.
    /// </summary>
    /// <param name="message">The received message containing the transaction data.</param>
    private async Task ProcessMessageAsync(ReceivedMessage<Null, string> message)
    {
        _logger.LogInformation("Received message: {MessageValue}", message.Value);

        // Create a new dependency scope to resolve scoped services like the anomaly detector.
        using var scope = _serviceProvider.CreateScope();
        var anomalyDetector = scope.ServiceProvider.GetRequiredService<ITransactionAnomalyDetector>();
        var cosmosDbService = scope.ServiceProvider.GetRequiredService<ICosmosDbService>();

        try
        {
            // Deserialize the message content into a Transaction object.
            Transaction? kafkaTransaction = JsonSerializer.Deserialize<Transaction>(message.Value);
            if (kafkaTransaction is not null)
            {
                // Detect anomalies and enrich the transaction with the result.
                string? anomalyFlag = await anomalyDetector.DetectAsync(kafkaTransaction);
                var processedTransaction = kafkaTransaction with { AnomalyFlag = anomalyFlag };
                TransactionForCosmos transactionForCosmos = TransactionForCosmos.FromDomainTransaction(processedTransaction);

                // Store the processed transaction in the database.
                await cosmosDbService.AddTransactionAsync(transactionForCosmos);
                _logger.LogInformation("Successfully processed and stored transaction {TransactionId}", transactionForCosmos.id);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message: {MessageValue}", message.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while processing message.");
        }
    }

    /// <summary>
    /// Gracefully stops the worker by disposing the message consumer.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker is stopping. Disposing message consumer.");
        await _messageConsumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
