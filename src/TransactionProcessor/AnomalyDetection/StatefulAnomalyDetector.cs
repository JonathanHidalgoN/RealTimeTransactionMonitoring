using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;

namespace TransactionProcessor.AnomalyDetection;

public class StatefulAnomalyDetector : ITransactionAnomalyDetector
{
    private readonly IRedisCacheService _cache;
    private readonly IAnomalyEventPublisher _eventPublisher;
    private readonly ILogger<StatefulAnomalyDetector> _logger;

    public StatefulAnomalyDetector(
        IRedisCacheService cache,
        IAnomalyEventPublisher eventPublisher,
        ILogger<StatefulAnomalyDetector> logger)
    {
        _cache = cache;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<string?> DetectAsync(Transaction transaction)
    {
        string redisKey = $"account-stats:{transaction.SourceAccount.AccountId}";

        var stats = await _cache.GetAsync<AccountStats>(redisKey) ?? new AccountStats();

        string? anomalyFlag = null;
        if (stats.TransactionCount > 5 && transaction.Amount > (stats.AverageTransactionAmount * 10))
        {
            anomalyFlag = "HighValueDeviationAnomaly";
            _logger.LogWarning(
                "Stateful Anomaly Detected for transaction {TransactionId}. Amount {Amount} is >10x the average of {Average}",
                transaction.Id, transaction.Amount, stats.AverageTransactionAmount);

            await _eventPublisher.PublishAsync(transaction);
        }
        stats.TransactionCount++;
        var oldAverage = stats.AverageTransactionAmount;
        stats.AverageTransactionAmount += (transaction.Amount - oldAverage) / stats.TransactionCount;

        await _cache.SetAsync(redisKey, stats);

        return anomalyFlag;
    }
}
