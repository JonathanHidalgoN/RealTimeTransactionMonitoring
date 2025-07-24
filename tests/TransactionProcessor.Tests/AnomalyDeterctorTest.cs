using Moq;
using Microsoft.Extensions.Logging;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using TransactionProcessor.AnomalyDetection;

namespace TransactionProcessor.Tests;

public class HighValueTransactionAnomalyDetectorTests
{
    private readonly Mock<IAnomalyEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<AnomalyDetector>> _mockLogger;
    private readonly AnomalyDetector _detector;

    public HighValueTransactionAnomalyDetectorTests()
    {
        _mockEventPublisher = new Mock<IAnomalyEventPublisher>();
        _mockLogger = new Mock<ILogger<AnomalyDetector>>();
        _detector = new AnomalyDetector(_mockLogger.Object, _mockEventPublisher.Object);
    }

    [Fact]
    public async Task DetectAsync_WithHighValueTransaction_ShouldPublishEventAndReturnFlag()
    {
        var highValueTransaction = new Transaction("tx-123", 15000.00, 0, new Account("A"), new Account("B"), 
            TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"));

        var result = await _detector.DetectAsync(highValueTransaction);

        Assert.Equal("HighValueAnomaly", result);

        _mockEventPublisher.Verify(
            publisher => publisher.PublishAsync(highValueTransaction),
            Times.Once()
        );
    }

    [Fact]
    public async Task DetectAsync_WithNormalValueTransaction_ShouldNotPublishEventAndReturnNull()
    {
        var normalTransaction = new Transaction("tx-456", 500.00, 0, new Account("C"), new Account("D"),
            TransactionType.Purchase, MerchantCategory.Grocery, "Test Market", new Location("NYC", "NY", "US"));

        var result = await _detector.DetectAsync(normalTransaction);

        Assert.Null(result);

        _mockEventPublisher.Verify(
            publisher => publisher.PublishAsync(It.IsAny<Transaction>()),
            Times.Never()
        );
    }
}
