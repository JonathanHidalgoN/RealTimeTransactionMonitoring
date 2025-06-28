using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;

namespace TransactionProcessor.AnomalyDetection;

/// <summary>
/// An anomaly detector that uses historical account data to identify suspicious transactions.
/// </summary>
/// <remarks>
/// This detector maintains statistics for each account, such as the number of transactions
/// and the average transaction amount. It uses Redis to persist this state, making the
/// detection process stateful and more intelligent than a simple rule-based system.
/// </remarks>
public class StatefulAnomalyDetector : ITransactionAnomalyDetector
{
    private readonly IRedisCacheService _cache;
    private readonly IAnomalyEventPublisher _eventPublisher;
    private readonly ILogger<StatefulAnomalyDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatefulAnomalyDetector"/> class.
    /// </summary>
    /// <param name="cache">The cache service used to store and retrieve account statistics.</param>
    /// <param name="eventPublisher">The service used to publish events for anomalous transactions.</param>
    /// <param name="logger">The logger for recording information and warnings.</param>
    public StatefulAnomalyDetector(
        IRedisCacheService cache,
        IAnomalyEventPublisher eventPublisher,
        ILogger<StatefulAnomalyDetector> logger)
    {
        _cache = cache;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Detects anomalies by comparing a transaction against historical data for the source account.
    /// </summary>
    /// <remarks>
    /// An anomaly is currently flagged if the transaction amount is more than 10 times the account's
    /// historical average and the account has more than 5 previous transactions.
    /// </remarks>
    /// <param name="transaction">The transaction to analyze.</param>
    /// <returns>
    /// A string representing the anomaly type if detected (e.g., "HighValueDeviationAnomaly");
    /// otherwise, <c>null</c>.
    /// </returns>
    public async Task<string?> DetectAsync(Transaction transaction)
    {
        string redisKey = $"account-stats:{transaction.SourceAccount.AccountId}";

        // Retrieve the historical statistics for this account from Redis.
        // If no stats exist, create a new object.
        var stats = await _cache.GetAsync<AccountStats>(redisKey) ?? new AccountStats();

        string? anomalyFlag = null;

        // Rule: Flag as an anomaly if the account has some history and the amount is a high deviation from the average.
        if (stats.TransactionCount > 5 && transaction.Amount > (stats.AverageTransactionAmount * 10))
        {
            anomalyFlag = "HighValueDeviationAnomaly";
            _logger.LogWarning(
                "Stateful Anomaly Detected for transaction {TransactionId}. Amount {Amount} is >10x the average of {Average}",
                transaction.Id, transaction.Amount, stats.AverageTransactionAmount);

            // Publish an event to notify other parts of the system about the anomaly.
            await _eventPublisher.PublishAsync(transaction);
        }

        // Update the account's statistics with the current transaction's data.
        stats.TransactionCount++;
        var oldAverage = stats.AverageTransactionAmount;
        // Update the running average.
        stats.AverageTransactionAmount += (transaction.Amount - oldAverage) / stats.TransactionCount;

        // Save the updated statistics back to Redis.
        await _cache.SetAsync(redisKey, stats);

        return anomalyFlag;
    }
}
