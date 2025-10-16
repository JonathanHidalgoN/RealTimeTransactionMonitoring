using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using System.Text.Json;
using FluentAssertions;
using TxnProcessor = TransactionProcessor.Services.TransactionProcessor;

namespace TransactionProcessor.Tests.Services;

public class TransactionProcessorTests
{
    private readonly Mock<ILogger<TxnProcessor>> _mockLogger;
    private readonly Mock<ITransactionAnomalyDetector> _mockAnomalyDetector;
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly TxnProcessor _processor;

    public TransactionProcessorTests()
    {
        _mockLogger = new Mock<ILogger<TxnProcessor>>();
        _mockAnomalyDetector = new Mock<ITransactionAnomalyDetector>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        // Setup service scope factory to return our mock scope
        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        // Setup service provider to return our mock services
        _mockServiceProvider.Setup(x => x.GetService(typeof(ITransactionAnomalyDetector))).Returns(_mockAnomalyDetector.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ITransactionRepository))).Returns(_mockTransactionRepository.Object);

        _processor = new TxnProcessor(_mockLogger.Object, _mockServiceScopeFactory.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidMessage_ShouldProcessAndStoreTransaction()
    {
        var transaction = new Transaction("TXN123", 100, 123456, new Account("ACC1"), new Account("ACC2"),
            TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"));
        var message = new ReceivedMessage<object?, string>(null, JsonSerializer.Serialize(transaction));

        _mockAnomalyDetector.Setup(d => d.DetectAsync(It.IsAny<Transaction>()))
                           .ReturnsAsync((string?)null);

        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockAnomalyDetector.Verify(d => d.DetectAsync(It.Is<Transaction>(t => t.Id == "TXN123")), Times.Once);
        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.Is<Transaction>(t => t.Id == "TXN123" && t.AnomalyFlag == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithAnomalyDetected_ShouldEnrichTransactionWithAnomalyFlag()
    {
        var transaction = new Transaction("TXN123", 10000, 123456, new Account("ACC1"), new Account("ACC2"),
            TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"));
        var message = new ReceivedMessage<object?, string>(null, JsonSerializer.Serialize(transaction));

        _mockAnomalyDetector.Setup(d => d.DetectAsync(It.IsAny<Transaction>()))
                           .ReturnsAsync("HIGH_AMOUNT");

        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.Is<Transaction>(t =>
            t.Id == "TXN123" && t.AnomalyFlag == "HIGH_AMOUNT"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithInvalidJson_ShouldHandleGracefullyAndNotProcess()
    {
        var message = new ReceivedMessage<object?, string>(null, "invalid-json");

        // Should not throw - should handle gracefully
        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithNullTransaction_ShouldNotProcessTransaction()
    {
        var message = new ReceivedMessage<object?, string>(null, "null");

        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAnomalyDetector.Verify(d => d.DetectAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _processor.ProcessMessageAsync(null!, CancellationToken.None));

        exception.ParamName.Should().Be("message");
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenAnomalyDetectorThrows_ShouldRethrowException()
    {
        var transaction = new Transaction("TXN123", 100, 123456, new Account("ACC1"), new Account("ACC2"),
            TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"));
        var message = new ReceivedMessage<object?, string>(null, JsonSerializer.Serialize(transaction));

        _mockAnomalyDetector.Setup(d => d.DetectAsync(It.IsAny<Transaction>()))
                           .ThrowsAsync(new InvalidOperationException("Anomaly detector failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _processor.ProcessMessageAsync(message, CancellationToken.None));

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenRepositoryThrows_ShouldRethrowException()
    {
        var transaction = new Transaction("TXN123", 100, 123456, new Account("ACC1"), new Account("ACC2"),
            TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"));
        var message = new ReceivedMessage<object?, string>(null, JsonSerializer.Serialize(transaction));

        _mockAnomalyDetector.Setup(d => d.DetectAsync(It.IsAny<Transaction>()))
                           .ReturnsAsync((string?)null);
        _mockTransactionRepository.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
                                  .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _processor.ProcessMessageAsync(message, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[1,2,3]")]
    public async Task ProcessMessageAsync_WithInvalidJsonFormats_ShouldHandleGracefullyAndNotProcess(string invalidJson)
    {
        var message = new ReceivedMessage<object?, string>(null, invalidJson);

        // Should not throw - should handle gracefully
        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"invalid\": \"structure\"}")]
    public async Task ProcessMessageAsync_WithValidJsonButInvalidTransaction_ShouldHandleGracefullyAndNotProcess(string invalidTransaction)
    {
        var message = new ReceivedMessage<object?, string>(null, invalidTransaction);

        // Should not throw - should handle gracefully
        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("LOW_AMOUNT")]
    [InlineData("HIGH_AMOUNT")]
    [InlineData("SUSPICIOUS_LOCATION")]
    [InlineData("UNUSUAL_TIME")]
    public async Task ProcessMessageAsync_WithDifferentAnomalyFlags_ShouldEnrichTransactionCorrectly(string anomalyFlag)
    {
        var transaction = new Transaction("TXN123", 100, 123456, new Account("ACC1"), new Account("ACC2"),
            TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"));
        var message = new ReceivedMessage<object?, string>(null, JsonSerializer.Serialize(transaction));

        _mockAnomalyDetector.Setup(d => d.DetectAsync(It.IsAny<Transaction>()))
                           .ReturnsAsync(anomalyFlag);

        await _processor.ProcessMessageAsync(message, CancellationToken.None);

        _mockTransactionRepository.Verify(r => r.AddTransactionAsync(It.Is<Transaction>(t =>
            t.Id == "TXN123" && t.AnomalyFlag == anomalyFlag), It.IsAny<CancellationToken>()), Times.Once);
    }
}
