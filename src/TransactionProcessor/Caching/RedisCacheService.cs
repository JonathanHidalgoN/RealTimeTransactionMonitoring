using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace TransactionProcessor.Caching;

public class RedisCacheService : IRedisCacheService
{
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IDatabase _database;

    public RedisCacheService(IOptions<RedisSettings> redisSettings, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        try
        {
            var redisConnection = ConnectionMultiplexer.Connect(redisSettings.Value.ConnectionString!);
            _database = redisConnection.GetDatabase();
            _logger.LogInformation("Successfully connected to Redis cache.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Could not connect to Redis cache. This is a fatal error for the service.");
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var serializedValue = await _database.StringGetAsync(key);
        if (serializedValue.IsNullOrEmpty)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(serializedValue!);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        return await _database.StringSetAsync(key, serializedValue, expiry);
    }
}