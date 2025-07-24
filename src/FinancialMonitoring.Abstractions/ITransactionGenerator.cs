using FinancialMonitoring.Models;

namespace FinancialMonitoring.Abstractions;

/// <summary>
/// Interface for generating realistic financial transactions
/// </summary>
public interface ITransactionGenerator
{
    /// <summary>
    /// Generates a realistic transaction with proper patterns and behaviors
    /// </summary>
    /// <returns>A realistic Transaction instance</returns>
    Transaction GenerateTransaction();
}