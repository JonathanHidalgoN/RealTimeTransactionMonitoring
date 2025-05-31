using FinancialMonitoring.Models; // For the Transaction type

namespace FinancialMonitoring.Abstractions.Services;

public interface ITransactionAnomalyDetector
{
    /// Detects if a given transaction is anomalous.
    Task<string?> DetectAsync(Transaction transaction);
}