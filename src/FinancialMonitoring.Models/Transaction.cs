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
    /// The Unix timestamp (in milliseconds) when the transaction occurred.
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
    /// The type of transaction (Purchase, Transfer, etc.)
    /// </summary>
    public TransactionType Type { get; init; }

    /// <summary>
    /// The merchant category for the transaction
    /// </summary>
    public MerchantCategory MerchantCategory { get; init; }

    /// <summary>
    /// The name of the merchant or entity
    /// </summary>
    public string MerchantName { get; init; }

    /// <summary>
    /// The location where the transaction occurred
    /// </summary>
    public Location Location { get; init; }

    /// <summary>
    /// The currency code (ISO 4217)
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// The payment method used for the transaction
    /// </summary>
    public PaymentMethod PaymentMethod { get; init; }

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
        TransactionType type,
        MerchantCategory merchantCategory,
        string merchantName,
        Location location,
        string currency = "USD",
        PaymentMethod paymentMethod = PaymentMethod.DebitCard,
        string? anomalyFlag = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Transaction ID cannot be null or whitespace.", nameof(id));
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(merchantName))
            throw new ArgumentException("Merchant name cannot be null or whitespace.", nameof(merchantName));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or whitespace.", nameof(currency));

        ArgumentNullException.ThrowIfNull(sourceAccount);
        ArgumentNullException.ThrowIfNull(destinationAccount);
        ArgumentNullException.ThrowIfNull(location);

        Id = id;
        Amount = amount;
        Timestamp = timestamp;
        SourceAccount = sourceAccount;
        DestinationAccount = destinationAccount;
        Type = type;
        MerchantCategory = merchantCategory;
        MerchantName = merchantName;
        Location = location;
        Currency = currency;
        PaymentMethod = paymentMethod;
        AnomalyFlag = anomalyFlag;
    }
}
