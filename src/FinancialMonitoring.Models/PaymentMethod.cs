namespace FinancialMonitoring.Models;

/// <summary>
/// Payment method used for the transaction
/// </summary>
public enum PaymentMethod
{
    DebitCard,
    CreditCard,
    ACH,
    Wire,
    Check,
    Cash,
    DigitalWallet,
    BankTransfer,
    Cryptocurrency
}
