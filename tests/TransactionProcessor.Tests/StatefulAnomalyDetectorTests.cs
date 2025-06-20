using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using TransactionProcessor.AnomalyDetection;

namespace TransactionProcessor.Tests;

public class StatefulAnomalyDetectorTests
{
    private readonly Mock<IRedisCacheService> _mockCache;
    private readonly Mock<IAnomalyEventPublisher> _mockEventPublisher;
    private readonly StatefulAnomalyDetector _detector;

    public StatefulAnomalyDetectorTests()
    {
        _mockCache = new Mock<IRedisCacheService>();
        _mockEventPublisher = new Mock<IAnomalyEventPublisher>();
        var mockLogger = Mock.Of<ILogger<StatefulAnomalyDetector>>();

        _detector = new StatefulAnomalyDetector(_mockCache.Object, _mockEventPublisher.Object, mockLogger);
    }

    private Transaction CreateTransaction(string accountId, double amount)
    {
        return new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: amount,
            timestamp: 0,
            sourceAccount: new Account(accountId),
            destinationAccount: new Account("DEST_ACC")
        );
    }

    [Fact]
    public async Task DetectAsync_WhenNoPriorStatsExist_ReturnsNoAnomalyAndSavesNewStats()
    {
        var transaction = CreateTransaction("ACC_NEW", 500.0);
        var expectedRedisKey = $"account-stats:{transaction.SourceAccount.AccountId}";

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

    [Fact]
    public async Task DetectAsync_WithNormalSubsequentTransaction_ReturnsNoAnomalyAndUpdatesStats()
    {
        var transaction = CreateTransaction("ACC_EXISTING", 1000.0);
        var expectedRedisKey = $"account-stats:{transaction.SourceAccount.AccountId}";

        var existingStats = new AccountStats { TransactionCount = 5, AverageTransactionAmount = 500.0 };

        _mockCache.Setup(c => c.GetAsync<AccountStats>(expectedRedisKey))
                  .ReturnsAsync(existingStats);

        var result = await _detector.DetectAsync(transaction);

        Assert.Null(result);

        _mockCache.Verify(c => c.SetAsync(
            expectedRedisKey,
            It.Is<AccountStats>(stats => stats.TransactionCount == 6 && Math.Abs(stats.AverageTransactionAmount - 583.33) < 0.01),
            It.IsAny<TimeSpan?>()),
            Times.Once());

        _mockEventPublisher.Verify(p => p.PublishAsync(It.IsAny<Transaction>()), Times.Never());
    }

    [Fact]
    public async Task DetectAsync_WithAnomalousTransaction_ReturnsFlagAndPublishesEvent()
    {
        var transaction = CreateTransaction("ACC_ANOMALY", 12000.0);
        var expectedRedisKey = $"account-stats:{transaction.SourceAccount.AccountId}";
        var existingStats = new AccountStats { TransactionCount = 6, AverageTransactionAmount = 500.0 };

        _mockCache.Setup(c => c.GetAsync<AccountStats>(expectedRedisKey))
                  .ReturnsAsync(existingStats);

        var result = await _detector.DetectAsync(transaction);

        Assert.Equal("HighValueDeviationAnomaly", result);

        _mockCache.Verify(c => c.SetAsync(
            expectedRedisKey,
            It.Is<AccountStats>(stats => stats.TransactionCount == 7),
            It.IsAny<TimeSpan?>()),
            Times.Once());

        _mockEventPublisher.Verify(
            p => p.PublishAsync(transaction),
            Times.Once()
        );
    }

}