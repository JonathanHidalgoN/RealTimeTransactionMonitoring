namespace FinancialMonitoring.Models;

/// <summary>
/// Represents the type of financial transaction
/// </summary>
public enum TransactionType
{
    Purchase,
    Transfer,
    ATMWithdrawal,
    Deposit,
    Refund,
    Fee,
    Interest,
    DividendPayment,
    BillPayment,
    Subscription
}
