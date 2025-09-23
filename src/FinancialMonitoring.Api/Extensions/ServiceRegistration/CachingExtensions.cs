using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Extensions.ServiceRegistration;

public static class CachingExtensions
{
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("CachingConfiguration");

        var cacheSettings = configuration.GetSection(AppConstants.CacheSettingsConfigPrefix).Get<CacheSettings>() ?? new CacheSettings();
        var responseCacheSettings = configuration.GetSection(AppConstants.ResponseCacheSettingsConfigPrefix).Get<ResponseCacheSettings>() ?? new ResponseCacheSettings();

        services.AddOptions<CacheSettings>()
            .Bind(configuration.GetSection(AppConstants.CacheSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ResponseCacheSettings>()
            .Bind(configuration.GetSection(AppConstants.ResponseCacheSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = responseCacheSettings.MaximumBodySizeBytes;
            options.UseCaseSensitivePaths = responseCacheSettings.UseCaseSensitivePaths;
            options.SizeLimit = responseCacheSettings.SizeLimitBytes;
        });

        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.ConditionalExpire(cacheSettings.BasePolicyCacheSeconds));

            options.AddPolicy(AppConstants.TransactionCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.TransactionCacheSeconds,
                    b => b.SetVaryByQuery("pageNumber", "pageSize", "startDate", "endDate", "minAmount", "maxAmount")));

            options.AddPolicy(AppConstants.TransactionByIdCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.TransactionByIdCacheSeconds,
                    b => b.SetVaryByRouteValue("id")));

            options.AddPolicy(AppConstants.AnomalousTransactionCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.AnomalousTransactionCacheSeconds,
                    b => b.SetVaryByQuery("pageNumber", "pageSize")));

            options.AddPolicy(AppConstants.AnalyticsCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.AnalyticsCacheSeconds));

            options.AddPolicy(AppConstants.TimeSeriesCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.TimeSeriesCacheSeconds,
                    b => b.SetVaryByQuery("hours", "intervalMinutes")));
        });

        logger.LogInformation("Cache Configuration: Response Cache={ResponseCacheMaxMB}MB max body, {ResponseCacheTotalMB}MB total, Transaction Cache={TransactionCache}, Transaction By ID Cache={TransactionByIdCache}, Anomalous Transaction Cache={AnomalousCache}, Analytics Cache={AnalyticsCache}, Time Series Cache={TimeSeriesCache}",
            responseCacheSettings.MaximumBodySizeMB, responseCacheSettings.SizeLimitMB,
            cacheSettings.TransactionCacheSeconds > 0 ? $"{cacheSettings.TransactionCacheSeconds}s" : "disabled",
            cacheSettings.TransactionByIdCacheSeconds > 0 ? $"{cacheSettings.TransactionByIdCacheSeconds}s" : "disabled",
            cacheSettings.AnomalousTransactionCacheSeconds > 0 ? $"{cacheSettings.AnomalousTransactionCacheSeconds}s" : "disabled",
            cacheSettings.AnalyticsCacheSeconds > 0 ? $"{cacheSettings.AnalyticsCacheSeconds}s" : "disabled",
            cacheSettings.TimeSeriesCacheSeconds > 0 ? $"{cacheSettings.TimeSeriesCacheSeconds}s" : "disabled");

        return services;
    }
}
