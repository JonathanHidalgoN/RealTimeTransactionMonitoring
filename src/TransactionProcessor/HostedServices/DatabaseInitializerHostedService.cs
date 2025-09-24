using FinancialMonitoring.Abstractions.Persistence;

namespace TransactionProcessor.HostedServices;

/// <summary>
/// A hosted service responsible for initializing the database and its containers/collections at application startup.
/// </summary>
public class DatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database Initializer Hosted Service starting initialization.");

        await using var scope = _serviceProvider.CreateAsyncScope();
        var transactionRepository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        await transactionRepository.InitializeAsync();

        _logger.LogInformation("Database Initializer Hosted Service has completed startup tasks.");
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database Initializer Hosted Service stopping.");
        return Task.CompletedTask;
    }
}
