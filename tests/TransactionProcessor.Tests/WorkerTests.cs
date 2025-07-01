using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using System.Text.Json;
using Confluent.Kafka;

namespace TransactionProcessor.Tests;

public class WorkerTests
{
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IMessageConsumer<Null, string>> _mockMessageConsumer;
    private readonly Mock<ITransactionAnomalyDetector> _mockAnomalyDetector;
    private readonly Mock<ICosmosDbService> _mockCosmosDbService;
    private readonly IServiceProvider _serviceProvider;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockMessageConsumer = new Mock<IMessageConsumer<Null, string>>();
        _mockAnomalyDetector = new Mock<ITransactionAnomalyDetector>();
        _mockCosmosDbService = new Mock<ICosmosDbService>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(provider => _mockAnomalyDetector.Object);
        serviceCollection.AddScoped(provider => _mockCosmosDbService.Object);
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidMessage_ShouldProcessAndStoreTransaction()
    {
        var transaction = new Transaction("TXN123", 100, 123456, new Account("ACC1"), new Account("ACC2"));
        var message = new ReceivedMessage<Null, string>(null, JsonSerializer.Serialize(transaction));

        _mockAnomalyDetector.Setup(d => d.DetectAsync(It.IsAny<Transaction>()))
                            .ReturnsAsync((string)null);

        var worker = new Worker(_mockLogger.Object, _serviceProvider, _mockMessageConsumer.Object);

        // This is a bit of a workaround to test the private method ProcessMessageAsync
        // In a real-world scenario, we might refactor this to be more easily testable.
        var method = typeof(Worker).GetMethod("ProcessMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(worker, new object[] { message });

        _mockAnomalyDetector.Verify(d => d.DetectAsync(It.IsAny<Transaction>()), Times.Once);
        _mockCosmosDbService.Verify(c => c.AddTransactionAsync(It.IsAny<TransactionForCosmos>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithDeserializationError_ShouldLogError()
    {
        var message = new ReceivedMessage<Null, string>(null, "invalid-json");

        var worker = new Worker(_mockLogger.Object, _serviceProvider, _mockMessageConsumer.Object);

        var method = typeof(Worker).GetMethod("ProcessMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(worker, new object[] { message });

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error deserializing message")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
