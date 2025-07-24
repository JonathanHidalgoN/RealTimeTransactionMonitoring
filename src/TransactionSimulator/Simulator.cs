using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Confluent.Kafka;
using System.IO;

namespace TransactionSimulator;

public class Simulator : BackgroundService
{
    private readonly ILogger<Simulator> _logger;
    private readonly IMessageProducer<Null, string> _messageProducer;
    private readonly ITransactionGenerator _transactionGenerator;

    public Simulator(ILogger<Simulator> logger, IMessageProducer<Null, string> messageProducer, ITransactionGenerator transactionGenerator)
    {
        _logger = logger;
        _messageProducer = messageProducer;
        _transactionGenerator = transactionGenerator;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transaction Simulator engine starting (as BackgroundService)...");
        int transactionCounter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            transactionCounter++;
            Transaction transaction = _transactionGenerator.GenerateTransaction();
            string jsonTransaction = JsonSerializer.Serialize(transaction);

            try
            {
                File.AppendAllText("/tmp/healthy", string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update liveness probe timestamp file.");
            }

            try
            {
                await _messageProducer.ProduceAsync(null, jsonTransaction, stoppingToken);

                _logger.LogInformation("[{Timestamp:HH:mm:ss}] Produced realistic transaction {Counter}: {AccountId} -> {MerchantName} (${Amount}) in {Location}",
                    DateTime.Now, transactionCounter, transaction.SourceAccount.AccountId,
                    transaction.MerchantName, transaction.Amount, transaction.Location.City);
            }
            catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
            {
                _logger.LogInformation("Message production loop was canceled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to produce message.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Simulation delay canceled. Exiting loop.");
                break;
            }
        }

        _logger.LogInformation("Transaction Simulator engine stopped.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Disposing message producer...");
        await _messageProducer.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
