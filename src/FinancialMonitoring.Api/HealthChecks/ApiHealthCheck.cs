using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;

namespace FinancialMonitoring.Api.HealthChecks;

/// <summary>
/// Health check for API service status and configuration
/// </summary>
/// https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-9.0
public class ApiHealthCheck : IHealthCheck
{
    private readonly ILogger<ApiHealthCheck> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiHealthCheck(ILogger<ApiHealthCheck> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting API health check");

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var data = new Dictionary<string, object>
            {
                ["version"] = version,
                ["environment"] = _environment.EnvironmentName,
                ["uptime"] = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["machineName"] = Environment.MachineName,
                ["processId"] = Environment.ProcessId,
                ["workingSet"] = $"{Environment.WorkingSet / 1024 / 1024} MB"
            };

            var workingSetMB = Environment.WorkingSet / 1024 / 1024;
            if (workingSetMB > 1000) // More than 1GB
            {
                _logger.LogWarning("API health check shows high memory usage: {WorkingSet} MB", workingSetMB);
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High memory usage detected ({workingSetMB} MB)", 
                    data: data));
            }

            _logger.LogDebug("API health check completed successfully");
            return Task.FromResult(HealthCheckResult.Healthy(
                "API service is running normally", 
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "API service health check failed", 
                ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }));
        }
    }
}