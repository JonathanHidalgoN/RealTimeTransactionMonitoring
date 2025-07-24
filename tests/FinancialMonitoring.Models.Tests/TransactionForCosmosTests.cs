namespace FinancialMonitoring.Models.Tests;

public class TransactionForCosmosTests
{
    [Fact]
    public void FromDomainTransaction_ShouldCorrectlyMapToCosmosModel()
    {
        var domainTransaction = new Transaction(
            id: "TXN123",
            amount: 150.75,
            timestamp: 1672531200,
            sourceAccount: new Account("SRC001"),
            destinationAccount: new Account("DEST002"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: "Test Store",
            location: new Location("New York", "NY", "US", "10001"),
            currency: "USD",
            paymentMethod: PaymentMethod.CreditCard,
            anomalyFlag: "HighValue");

        var cosmosTransaction = TransactionForCosmos.FromDomainTransaction(domainTransaction);

        Assert.Equal(domainTransaction.Id, cosmosTransaction.id);
        Assert.Equal(domainTransaction.Amount, cosmosTransaction.Amount);
        Assert.Equal(domainTransaction.Timestamp, cosmosTransaction.Timestamp);
        Assert.Equal(domainTransaction.SourceAccount, cosmosTransaction.SourceAccount);
        Assert.Equal(domainTransaction.DestinationAccount, cosmosTransaction.DestinationAccount);
        Assert.Equal(domainTransaction.AnomalyFlag, cosmosTransaction.AnomalyFlag);
    }

    [Fact]
    public void ToTransaction_ShouldCorrectlyMapToDomainModel()
    {
        var cosmosTransaction = new TransactionForCosmos
        {
            id = "COSMOS_TXN456",
            Amount = 99.99,
            Timestamp = 1672534800,
            SourceAccount = new Account("SRC003"),
            DestinationAccount = new Account("DEST004"),
            Type = TransactionType.Purchase,
            MerchantCategory = MerchantCategory.Grocery,
            MerchantName = "Test Market",
            Location = new Location("Los Angeles", "CA", "US", "90210"),
            Currency = "USD",
            PaymentMethod = PaymentMethod.DebitCard,
            AnomalyFlag = null
        };

        var domainTransaction = cosmosTransaction.ToTransaction();

        Assert.Equal(cosmosTransaction.id, domainTransaction.Id);
        Assert.Equal(cosmosTransaction.Amount, domainTransaction.Amount);
        Assert.Equal(cosmosTransaction.Timestamp, domainTransaction.Timestamp);
        Assert.Equal(cosmosTransaction.SourceAccount, domainTransaction.SourceAccount);
        Assert.Equal(cosmosTransaction.DestinationAccount, domainTransaction.DestinationAccount);
        Assert.Equal(cosmosTransaction.AnomalyFlag, domainTransaction.AnomalyFlag);
    }
}
