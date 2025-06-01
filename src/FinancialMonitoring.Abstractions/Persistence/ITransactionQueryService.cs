using FinancialMonitoring.Models;

namespace FinancialMonitoring.Abstractions.Persistence;

public interface ITransactionQueryService
{
    Task<IEnumerable<Transaction>> GetAllTransactionsAsync(int pageNumber = 1, int pageSize = 20);
    Task<Transaction?> GetTransactionByIdAsync(string id);
    Task<IEnumerable<Transaction>> GetAnomalousTransactionsAsync(int pageNumber = 1, int pageSize = 20);
}
