using FinancialMonitoring.Models;
using System.Threading.Tasks;

namespace FinancialMonitoring.Abstractions.Messaging;

public interface IAnomalyEventPublisher
{
    Task PublishAsync(Transaction anomalousTransaction);
}