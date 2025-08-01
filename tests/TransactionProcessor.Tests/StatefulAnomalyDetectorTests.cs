using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using TransactionProcessor.AnomalyDetection;
using Microsoft.Extensions.Options;

namespace TransactionProcessor.Tests;

public class StatefulAnomalyDetectorTests
{
    private readonly Mock<IRedisCacheService> _mockCache;
    private readonly Mock<IAnomalyEventPublisher> _mockEventPublisher;
    private readonly StatefulAnomalyDetector _detector;
    private readonly AnomalyDetectionSettings _settings;

    public StatefulAnomalyDetectorTests()
    {
        _mockCache = new Mock<IRedisCacheService>();
        _mockEventPublisher = new Mock<IAnomalyEventPublisher>();
        var mockLogger = Mock.Of<ILogger<StatefulAnomalyDetector>>();

        // Create default settings for the tests.
        _settings = new AnomalyDetectionSettings();
        var mockOptions = new Mock<IOptions<AnomalyDetectionSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_settings);

        _detector = new StatefulAnomalyDetector(_mockCache.Object, _mockEventPublisher.Object, mockLogger, mockOptions.Object);
    }

    private Transaction CreateTransaction(string accountId, double amount)
    {
        return new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: amount,
            timestamp: 0,
            sourceAccount: new Account(accountId),
            destinationAccount: new Account("DEST_ACC"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: "Test Store",
            location: new Location("NYC", "NY", "US")
        );
    }

    /// <summary>
    /// This test verifies that when no account stats exist, the first transaction returns no anomaly and saves initial stats
    /// </summary>
    [Fact]
    public async Task DetectAsync_WhenNoPriorStatsExist_ReturnsNoAnomalyAndSavesNewStats()
    {
        var transaction = CreateTransaction("ACC_NEW", 500.0);
        var expectedRedisKey = $"{_settings.AccountStatsKeyPrefix}{transaction.SourceAccount.AccountId}";

        _mockCache.Setup(c => c.GetAsync<AccountStats>(expectedRedisKey))
                  .ReturnsAsync((AccountStats?)null);

        var result = await _detector.DetectAsync(transaction);

        Assert.Null(result);

        _mockCache.Verify(c => c.SetAsync(
            expectedRedisKey,
            It.Is<AccountStats>(stats => stats.TransactionCount == 1 && stats.AverageTransactionAmount == 500.0),
            It.IsAny<TimeSpan?>()),
            Times.Once());
        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<Transaction>()), Times.Never());
    }

    /// <summary>
    /// This test verifies that normal subsequent transactions return no anomaly and update account statistics
    /// </summary>
    [Fact]
    public async Task DetectAsync_WithNormalSubsequentTransaction_ReturnsNoAnomalyAndUpdatesStats()
    {
        var transaction = CreateTransaction("ACC_EXISTING", 1000.0);
        var expectedRedisKey = $"{_settings.AccountStatsKeyPrefix}{transaction.SourceAccount.AccountId}";

        // Ensure the transaction is not anomalous based on the default settings.
        var existingStats = new AccountStats { TransactionCount = _settings.MinimumTransactionCount, AverageTransactionAmount = 500.0 };

        _mockCache.Setup(c => c.GetAsync<AccountStats>(expectedRedisKey))
                  .ReturnsAsync(existingStats);

        var result = await _detector.DetectAsync(transaction);

        Assert.Null(result);

        _mockCache.Verify(c => c.SetAsync(
            expectedRedisKey,
            It.Is<AccountStats>(stats => stats.TransactionCount == _settings.MinimumTransactionCount + 1),
            It.IsAny<TimeSpan?>()),
            Times.Once());

        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<Transaction>()), Times.Never());
    }

    /// <summary>
    /// This test verifies that anomalous transactions return appropriate flags and publish events for notification
    /// </summary>
    [Fact]
    public async Task DetectAsync_WithAnomalousTransaction_ReturnsFlagAndPublishesEvent()
    {
        var transaction = CreateTransaction("ACC_ANOMALY", 12000.0);
        var expectedRedisKey = $"{_settings.AccountStatsKeyPrefix}{transaction.SourceAccount.AccountId}";
        var existingStats = new AccountStats { TransactionCount = _settings.MinimumTransactionCount + 1, AverageTransactionAmount = 500.0 };

        _mockCache.Setup(c => c.GetAsync<AccountStats>(expectedRedisKey))
                  .ReturnsAsync(existingStats);

        var result = await _detector.DetectAsync(transaction);

        Assert.Equal(_settings.HighValueDeviationAnomalyFlag, result);

        _mockCache.Verify(c => c.SetAsync(
            expectedRedisKey,
            It.Is<AccountStats>(stats => stats.TransactionCount == _settings.MinimumTransactionCount + 2),
            It.IsAny<TimeSpan?>()),
            Times.Once());

        _mockEventPublisher.Verify(
            p => p.PublishAsync(transaction),
            Times.Once()
        );
    }
}
