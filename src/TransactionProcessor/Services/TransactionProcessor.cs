using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using System.Text.Json;

namespace TransactionProcessor.Services;

public class TransactionProcessor : ITransactionProcessor
{
    private readonly ILogger<TransactionProcessor> _logger;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionAnomalyDetector _transactionAnomalyDetector;

    public TransactionProcessor(ILogger<TransactionProcessor> logger,
            ITransactionRepository transactionRepository, ITransactionAnomalyDetector transactionAnomalyDetector)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _transactionAnomalyDetector = transactionAnomalyDetector;
    }

    public async Task ProcessMessageAsync(ReceivedMessage<object?, string> message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation("Received message: {MessageValue}", message.Value);

        try
        {
            Transaction? kafkaTransaction = JsonSerializer.Deserialize<Transaction>(message.Value);
            if (kafkaTransaction is not null)
            {
                string? anomalyFlag = await _transactionAnomalyDetector.DetectAsync(kafkaTransaction);
                var processedTransaction = kafkaTransaction with { AnomalyFlag = anomalyFlag };

                await _transactionRepository.AddTransactionAsync(processedTransaction, cancellationToken);
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
