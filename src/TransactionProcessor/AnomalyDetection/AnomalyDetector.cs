using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;

namespace TransactionProcessor.AnomalyDetection;

public class AnomalyDetector : ITransactionAnomalyDetector
{
    private readonly ILogger<AnomalyDetector> _logger;
    private readonly IAnomalyEventPublisher _eventPublisher;
    private const double _highValueThreshold = 1000.00;

    public AnomalyDetector(ILogger<AnomalyDetector> logger, IAnomalyEventPublisher publisher)
    {
        _logger = logger;
        _eventPublisher = publisher;
    }

    public async Task<string?> DetectAsync(Transaction transaction)
    {
        if (transaction.Amount > _highValueThreshold)
        {
            _logger.LogWarning("High value anomaly detected for transaction {TransactionId} with amount {Amount}",
                transaction.Id, transaction.Amount);
            await _eventPublisher.PublishAsync(transaction);

            return "HighValueAnomaly";
        }
        return null;
    }
}
