namespace FinancialMonitoring.Models;

public record Transaction(
    double Amount,
    string TransactionId, 
    long Timestamp,
    string SourceAccount,
    string DestinationAccount,
    string? AnomalyFlag = null
    );
