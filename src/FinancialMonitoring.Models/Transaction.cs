namespace FinancialMonitoring.Models;

public record Transaction{
    public string TransactionId { get; init; }
    public double Amount { get; init; }
    public long Timestamp { get; init; }
    public Account SourceAccount { get; init; }
    public Account DestinationAccount { get; init; }
    public string? AnomalyFlag { get; init; }

    public Transaction(
        string transactionId,
        double amount,
        long timestamp,
        Account sourceAccount,
        Account destinationAccount,
        string? anomalyFlag = null)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID cannot be null or whitespace.", nameof(transactionId));
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        
        // The Account objects themselves should be validated upon their creation.
        ArgumentNullException.ThrowIfNull(sourceAccount);
        ArgumentNullException.ThrowIfNull(destinationAccount);

        TransactionId = transactionId;
        Amount = amount;
        Timestamp = timestamp;
        SourceAccount = sourceAccount;
        DestinationAccount = destinationAccount;
        AnomalyFlag = anomalyFlag;
    }
}
