using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;

namespace TransactionProcessor;

/// <summary>
/// The main background service responsible for processing financial transactions.
/// </summary>
/// <remarks>
/// This worker consumes messages from a message broker, deserializes them, detects anomalies,
/// and persists the results to a data store. It runs as a long-running background task.
/// </remarks>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageConsumer<object?, string> _messageConsumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording operational information.</param>
    /// <param name="serviceProvider">The service provider for creating scoped services.</param>
    /// <param name="messageConsumer">The message consumer to receive transactions from.</param>
    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IMessageConsumer<object?, string> messageConsumer)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageConsumer = messageConsumer;
    }

    /// <summary>
    /// Executes the main logic of the worker, which is to start the message consumption loop.
    /// </summary>
    /// <param name="stoppingToken">A token that signals when the worker should stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting consumption loop.");

        await _messageConsumer.ConsumeAsync(async message =>
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var transactionProcessor = scope.ServiceProvider.GetRequiredService<ITransactionProcessor>();
            await transactionProcessor.ProcessMessageAsync(message, stoppingToken);
        }, stoppingToken);

        _logger.LogInformation("Worker consumption loop finished.");
    }


    /// <summary>
    /// Gracefully stops the worker by disposing the message consumer.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker is stopping. Disposing message consumer.");
        await _messageConsumer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
