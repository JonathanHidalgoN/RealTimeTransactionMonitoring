namespace FinancialMonitoring.Models;

/// <summary>
/// Holds statistical data about a financial account for stateful anomaly detection.
/// </summary>
public class AccountStats
{
    /// <summary>
    /// The total number of transactions recorded for the account.
    /// </summary>
    public int TransactionCount { get; set; } = 0;

    /// <summary>
    /// The running average of the transaction amounts for the account.
    /// </summary>
    public double AverageTransactionAmount { get; set; } = 0.0;
}
