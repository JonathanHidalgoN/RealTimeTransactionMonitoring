using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessor.HostedServices;
using FinancialMonitoring.Abstractions.Persistence;

namespace TransactionProcessor.Tests.HostedServices;

public class DatabaseInitializerHostedServiceTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();

        var action = () => new DatabaseInitializerHostedService(serviceProvider, logger);

        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldAcceptParameter()
    {
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();

        var action = () => new DatabaseInitializerHostedService(null!, logger);

        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldAcceptParameter()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();

        var action = () => new DatabaseInitializerHostedService(serviceProvider, null!);

        action.Should().NotThrow();
    }

    [Fact]
    public async Task StartAsync_WithMockRepository_ShouldCallInitializeAsync()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);

        await service.StartAsync(CancellationToken.None);

        mockRepository.Verify(x => x.InitializeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(x => x.InitializeAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);

        var action = async () => await service.StartAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_ShouldHandleCancellationToken()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var action = async () => await service.StartAsync(cancellationTokenSource.Token);

        await action.Should().NotThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);

        var action = async () => await service.StopAsync(CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldHandleCancellationToken()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var action = async () => await service.StopAsync(cancellationTokenSource.Token);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_ShouldLogInformationMessages()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var mockLogger = new Mock<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, mockLogger.Object);

        await service.StartAsync(CancellationToken.None);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting initialization")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed startup tasks")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogInformationMessage()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var mockLogger = new Mock<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, mockLogger.Object);

        await service.StopAsync(CancellationToken.None);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopping")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static IServiceProvider CreateServiceProvider(ITransactionRepository repository)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(repository);
        return serviceCollection.BuildServiceProvider();
    }
}
