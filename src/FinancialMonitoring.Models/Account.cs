namespace FinancialMonitoring.Models;

public record Account
{
  public string AccountId{get; init;}

  public Account(string accountId){
    if(string.IsNullOrWhiteSpace(accountId)){
      throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
    } 
    AccountId = accountId;
  }
}
