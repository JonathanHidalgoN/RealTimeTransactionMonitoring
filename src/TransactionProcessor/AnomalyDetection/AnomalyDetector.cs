using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;

namespace TransactionProcessor.AnomalyDetection;

public class AnomalyDetector : ITransactionAnomalyDetector
{
    private readonly ILogger<AnomalyDetector> _logger;
    private const double _highValueThreshold = 10000.00;

    public AnomalyDetector(ILogger<AnomalyDetector> logger)
    {
        _logger = logger;
    }

    public Task<string?> DetectAsync(Transaction transaction)
    {
        if (transaction.Amount > _highValueThreshold)
        {
            _logger.LogWarning("High value anomaly detected for transaction {TransactionId} with amount {Amount}",
                transaction.Id, transaction.Amount);
            return Task.FromResult<string?>("HighValueAnomaly");
        }
        return Task.FromResult<string?>(null);
    }
}