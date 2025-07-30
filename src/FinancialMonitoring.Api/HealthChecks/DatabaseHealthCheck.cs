using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancialMonitoring.Api.HealthChecks;

/// <summary>
/// Health check for database connectivity and basic operations
/// </summary>
/// https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-9.0
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ITransactionRepository transactionRepository, ILogger<DatabaseHealthCheck> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting database health check");

            // simple query
            var startTime = DateTime.UtcNow;
            var result = await _transactionRepository.GetAllTransactionsAsync(1, 1);
            var duration = DateTime.UtcNow - startTime;

            var data = new Dictionary<string, object>
            {
                ["responseTime"] = $"{duration.TotalMilliseconds:F2}ms",
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["queryExecuted"] = "GetAllTransactionsAsync(1, 1)"
            };

            if (duration.TotalSeconds > 5)
            {
                _logger.LogWarning("Database health check completed with slow response time: {Duration}ms", duration.TotalMilliseconds);
                return HealthCheckResult.Degraded(
                    $"Database responding slowly ({duration.TotalMilliseconds:F2}ms)", 
                    data: data);
            }

            _logger.LogDebug("Database health check completed successfully in {Duration}ms", duration.TotalMilliseconds);
            return HealthCheckResult.Healthy(
                $"Database responding normally ({duration.TotalMilliseconds:F2}ms)", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy(
                "Database connectivity failed", 
                ex, 
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                });
        }
    }
}