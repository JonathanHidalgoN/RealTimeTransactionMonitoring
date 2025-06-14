using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using System.Text.Json;
using Confluent.Kafka;
using FinancialMonitoring.Abstractions.Persistence;

namespace TransactionProcessor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageConsumer<Null, string> _messageConsumer;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IMessageConsumer<Null, string> messageConsumer)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageConsumer = messageConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting consumption loop.");
        await _messageConsumer.ConsumeAsync(ProcessMessageAsync, stoppingToken);
        _logger.LogInformation("Worker consumption loop finished.");
    }

    private async Task ProcessMessageAsync(ReceivedMessage<Null, string> message)
    {
        _logger.LogInformation("Received message: {MessageValue}", message.Value);

        using var scope = _serviceProvider.CreateScope();
        var anomalyDetector = scope.ServiceProvider.GetRequiredService<ITransactionAnomalyDetector>();
        var cosmosDbService = scope.ServiceProvider.GetRequiredService<ICosmosDbService>();

        try
        {
            Transaction? kafkaTransaction = JsonSerializer.Deserialize<Transaction>(message.Value);
            if (kafkaTransaction is not null)
            {
                string? anomalyFlag = await anomalyDetector.DetectAsync(kafkaTransaction);
                var processedTransaction = kafkaTransaction with { AnomalyFlag = anomalyFlag };
                TransactionForCosmos transactionForCosmos = TransactionForCosmos.FromDomainTransaction(processedTransaction);

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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker is stopping. Disposing message consumer.");
        await _messageConsumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}