using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models;

/// <summary>
/// Represents a single financial transaction within the domain.
/// </summary>
/// <remarks>
/// This record is the primary representation of a transaction used throughout the application logic.
/// It is immutable to ensure data integrity after creation.
/// </remarks>
public record Transaction
{
    /// <summary>
    /// The unique identifier for the transaction.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; }

    /// <summary>
    /// The monetary value of the transaction.
    /// </summary>
    public double Amount { get; init; }

    /// <summary>
    /// The Unix timestamp (in seconds) when the transaction occurred.
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    /// The account from which the funds are being transferred.
    /// </summary>
    public Account SourceAccount { get; init; }

    /// <summary>
    /// The account to which the funds are being transferred.
    /// </summary>
    public Account DestinationAccount { get; init; }

    /// <summary>
    /// A flag indicating the type of anomaly detected, if any.
    /// A <c>null</c> value means the transaction is not considered anomalous.
    /// </summary>
    public string? AnomalyFlag { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class.
    /// </summary>
    public Transaction(
        string id,
        double amount,
        long timestamp,
        Account sourceAccount,
        Account destinationAccount,
        string? anomalyFlag = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Transaction ID cannot be null or whitespace.", nameof(id));
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        ArgumentNullException.ThrowIfNull(sourceAccount);
        ArgumentNullException.ThrowIfNull(destinationAccount);

        Id = id;
        Amount = amount;
        Timestamp = timestamp;
        SourceAccount = sourceAccount;
        DestinationAccount = destinationAccount;
        AnomalyFlag = anomalyFlag;
    }
}
