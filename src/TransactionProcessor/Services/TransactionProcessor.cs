using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using System.Text.Json;

namespace TransactionProcessor.Services;

public class TransactionProcessor : ITransactionProcessor
{
    private readonly ILogger<TransactionProcessor> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TransactionProcessor(ILogger<TransactionProcessor> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ProcessMessageAsync(ReceivedMessage<object?, string> message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation("Received message: {MessageValue}", message.Value);

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var anomalyDetector = scope.ServiceProvider.GetRequiredService<ITransactionAnomalyDetector>();
        var transactionRepository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        try
        {
            Transaction? kafkaTransaction = JsonSerializer.Deserialize<Transaction>(message.Value);
            if (kafkaTransaction is not null)
            {
                string? anomalyFlag = await anomalyDetector.DetectAsync(kafkaTransaction);
                var processedTransaction = kafkaTransaction with { AnomalyFlag = anomalyFlag };

                await transactionRepository.AddTransactionAsync(processedTransaction);
                _logger.LogInformation("Successfully processed and stored transaction {TransactionId}", processedTransaction.Id);
            }
            else
            {
                _logger.LogWarning("Deserialized transaction was null for message: {MessageValue}", message.Value);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message: {MessageValue}", message.Value);
            // Don't crash the service for invalid JSON - just skip this message
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid transaction data in message: {MessageValue}", message.Value);
            // Don't crash the service for invalid transaction data - just skip this message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while processing message.");
            throw;
        }
    }
}
