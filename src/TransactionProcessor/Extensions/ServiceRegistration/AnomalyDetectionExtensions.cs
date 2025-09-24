using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using TransactionProcessor.AnomalyDetection;
using TransactionProcessor.Caching;

namespace TransactionProcessor.Extensions.ServiceRegistration;

public static class AnomalyDetectionExtensions
{
    public static IServiceCollection AddAnomalyDetection(this IServiceCollection services, IConfiguration configuration)
    {
        var anomalyDetectionMode = configuration["AnomalyDetection:Mode"]?.ToLowerInvariant() ?? "stateless";
        Console.WriteLine($"Configuring anomaly detection mode: {anomalyDetectionMode}");

        services.AddOptions<AnomalyDetectionSettings>()
            .Bind(configuration.GetSection("AnomalyDetection"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (anomalyDetectionMode == "stateful")
        {
            Console.WriteLine("Configuring stateful anomaly detection with Redis dependency");
            services.AddOptions<RedisSettings>()
                .Bind(configuration.GetSection(AppConstants.RedisDbConfigPrefix))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<IRedisCacheService, RedisCacheService>();
            services.AddScoped<ITransactionAnomalyDetector, StatefulAnomalyDetector>();
        }
        else
        {
            Console.WriteLine("Configuring stateless anomaly detection (no Redis dependency)");
            services.AddScoped<ITransactionAnomalyDetector, AnomalyDetector>();
        }

        return services;
    }
}
