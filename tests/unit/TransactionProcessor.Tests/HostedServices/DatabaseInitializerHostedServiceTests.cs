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
    public async Task StartingAsync_WithMockRepository_ShouldCallInitializeAsync()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);

        await service.StartingAsync(CancellationToken.None);

        mockRepository.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartingAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Test exception"));
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var logger = Mock.Of<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, logger);

        var action = async () => await service.StartingAsync(CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartingAsync_WhenCancelled_ShouldThrowOperationCancelledExceptio()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(
                x => x.InitializeAsync(
                    It.IsAny<CancellationToken>()
                    )
                ).ThrowsAsync(
                    new OperationCanceledException()
                    );
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var mockLogger = new Mock<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, mockLogger.Object);
        var cancellationTokenSource = new CancellationTokenSource();
        var action = async () => await service.StartingAsync(cancellationTokenSource.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StartingAsync_ShouldPassCancellationToken()
    {
        var mockRepository = new Mock<ITransactionRepository>();
        var serviceProvider = CreateServiceProvider(mockRepository.Object);
        var mockLogger = new Mock<ILogger<DatabaseInitializerHostedService>>();
        var service = new DatabaseInitializerHostedService(serviceProvider, mockLogger.Object);
        var cancellationTokenSource = new CancellationTokenSource();
        await service.StartingAsync(cancellationTokenSource.Token);
        mockRepository.Verify(x => x.InitializeAsync(cancellationTokenSource.Token), Times.Once);
    }

    private static IServiceProvider CreateServiceProvider(ITransactionRepository repository)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(repository);
        return serviceCollection.BuildServiceProvider();
    }
}
