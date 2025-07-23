using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models;

/// <summary>
/// Represents the data transfer object (DTO) for a transaction stored in Cosmos DB.
/// </summary>
/// <remarks>
/// This record is specifically designed for persistence in Cosmos DB and uses a lowercase `id`
/// to conform to the standard property naming for Cosmos DB documents.
/// </remarks>
public record TransactionForCosmos
{
    /// <summary>
    /// The unique identifier for the document in Cosmos DB.
    /// </summary>
    [JsonPropertyName("id")]
    public required string id { get; init; }

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
    public required Account SourceAccount { get; init; }

    /// <summary>
    /// The account to which the funds are being transferred.
    /// </summary>
    public required Account DestinationAccount { get; init; }

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
    public required string MerchantName { get; init; }

    /// <summary>
    /// The location where the transaction occurred
    /// </summary>
    public required Location Location { get; init; }

    /// <summary>
    /// The currency code (ISO 4217)
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// The payment method used for the transaction
    /// </summary>
    public PaymentMethod PaymentMethod { get; init; }

    /// <summary>
    /// A flag indicating the type of anomaly detected, if any.
    /// </summary>
    public string? AnomalyFlag { get; init; }

    /// <summary>
    /// Creates a <see cref="TransactionForCosmos"/> instance from a domain <see cref="Transaction"/> object.
    /// </summary>
    /// <param name="domainTransaction">The domain transaction to convert.</param>
    /// <returns>A new <see cref="TransactionForCosmos"/> instance.</returns>
    public static TransactionForCosmos FromDomainTransaction(FinancialMonitoring.Models.Transaction domainTransaction)
    {
        return new TransactionForCosmos
        {
            id = domainTransaction.Id,
            Amount = domainTransaction.Amount,
            Timestamp = domainTransaction.Timestamp,
            SourceAccount = domainTransaction.SourceAccount,
            DestinationAccount = domainTransaction.DestinationAccount,
            Type = domainTransaction.Type,
            MerchantCategory = domainTransaction.MerchantCategory,
            MerchantName = domainTransaction.MerchantName,
            Location = domainTransaction.Location,
            Currency = domainTransaction.Currency,
            PaymentMethod = domainTransaction.PaymentMethod,
            AnomalyFlag = domainTransaction.AnomalyFlag
        };
    }

    /// <summary>
    /// Converts this Cosmos DB object back to a domain <see cref="Transaction"/> object.
    /// </summary>
    /// <returns>A new <see cref="Transaction"/> instance.</returns>
    public Transaction ToTransaction()
    {
        return new Transaction(
            id,
            Amount,
            Timestamp,
            SourceAccount,
            DestinationAccount,
            Type,
            MerchantCategory,
            MerchantName,
            Location,
            Currency,
            PaymentMethod,
            AnomalyFlag
        );
    }
}
