using FinancialMonitoring.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialMonitoring.Abstractions.Persistence
{
    public interface ITransactionQueryService
    {
        Task<PagedResult<Transaction>?> GetAllTransactionsAsync(int pageNumber, int pageSize);
        Task<Transaction?> GetTransactionByIdAsync(string id);
        Task<IEnumerable<Transaction>> GetAnomalousTransactionsAsync();
    }
}
