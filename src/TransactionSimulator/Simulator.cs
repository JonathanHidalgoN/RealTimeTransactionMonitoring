using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Confluent.Kafka;
using System.IO;
using TransactionSimulator.Generation;

namespace TransactionSimulator;

public class Simulator : BackgroundService
{
    private readonly ILogger<Simulator> _logger;
    private readonly IMessageProducer<Null, string> _messageProducer;
    private readonly TransactionGenerator _transactionGenerator;

    public Simulator(ILogger<Simulator> logger, IMessageProducer<Null, string> messageProducer)
    {
        _logger = logger;
        _messageProducer = messageProducer;
        _transactionGenerator = new TransactionGenerator();
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transaction Simulator engine starting (as BackgroundService)...");
        int transactionCounter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            transactionCounter++;
            Transaction transaction = _transactionGenerator.GenerateRealisticTransaction();
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
