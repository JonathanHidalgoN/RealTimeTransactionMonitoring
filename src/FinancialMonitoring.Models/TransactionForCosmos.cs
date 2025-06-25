//A copy of transacion object but with 'id' in lower case because cosmos this object is
//only used to map from Transaction to cosmos database
using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models;

public record TransactionForCosmos
{
    [JsonPropertyName("id")]
    public required string id { get; init; }
    public double Amount { get; init; }
    public long Timestamp { get; init; }
    public required Account SourceAccount { get; init; }
    public required Account DestinationAccount { get; init; }
    public string? AnomalyFlag { get; init; }

    public static TransactionForCosmos FromDomainTransaction(FinancialMonitoring.Models.Transaction domainTransaction)
    {
        return new TransactionForCosmos
        {
            id = domainTransaction.Id,
            Amount = domainTransaction.Amount,
            Timestamp = domainTransaction.Timestamp,
            SourceAccount = domainTransaction.SourceAccount,
            DestinationAccount = domainTransaction.DestinationAccount,
            AnomalyFlag = domainTransaction.AnomalyFlag
        };
    }

    public Transaction ToTransaction()
    {
        return new Transaction(
            Id,
            Amount,
            Timestamp,
            SourceAccount,
            DestinationAccount,
            AnomalyFlag
        );
    }

}
