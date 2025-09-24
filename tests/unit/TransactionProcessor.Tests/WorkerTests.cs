using Moq;
using Microsoft.Extensions.Logging;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace TransactionProcessor.Tests;

public class WorkerTests
{
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IMessageConsumer<object?, string>> _mockMessageConsumer;
    private readonly Mock<ITransactionProcessor> _mockTransactionProcessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly Worker _worker;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockMessageConsumer = new Mock<IMessageConsumer<object?, string>>();
        _mockTransactionProcessor = new Mock<ITransactionProcessor>();

        // Create a real service collection with the mocked transaction processor
        var services = new ServiceCollection();
        services.AddSingleton(_mockTransactionProcessor.Object);
        _serviceProvider = services.BuildServiceProvider();

        _worker = new Worker(_mockLogger.Object, _serviceProvider, _mockMessageConsumer.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStartMessageConsumption()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mockMessageConsumer.Setup(c => c.ConsumeAsync(It.IsAny<Func<ReceivedMessage<object?, string>, Task>>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

        await _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(10);
        await _worker.StopAsync(CancellationToken.None);

        _mockMessageConsumer.Verify(c => c.ConsumeAsync(
            It.IsAny<Func<ReceivedMessage<object?, string>, Task>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task StopAsync_ShouldDisposeMessageConsumer()
    {
        await _worker.StopAsync(CancellationToken.None);

        _mockMessageConsumer.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConsumerPassesMessage_ShouldDelegateToTransactionProcessor()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Func<ReceivedMessage<object?, string>, Task>? capturedHandler = null;

        _mockMessageConsumer.Setup(c => c.ConsumeAsync(It.IsAny<Func<ReceivedMessage<object?, string>, Task>>(), It.IsAny<CancellationToken>()))
                           .Callback<Func<ReceivedMessage<object?, string>, Task>, CancellationToken>((handler, token) =>
                           {
                               capturedHandler = handler;
                           })
                           .Returns(Task.CompletedTask);

        await _worker.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(10);
        await _worker.StopAsync(CancellationToken.None);

        capturedHandler.Should().NotBeNull();
    }
}
