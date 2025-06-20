using System;
using System.Threading.Tasks;

namespace FinancialMonitoring.Abstractions.Caching;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null);
}