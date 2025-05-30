using FinancialMonitoring.Models;

namespace FinancialMonitoring.Abstractions.Persistence;
public interface ICosmosDbService
{
    Task AddTransactionAsync(TransactionForCosmos item);
    Task InitializeDatabaseAsync();
}
