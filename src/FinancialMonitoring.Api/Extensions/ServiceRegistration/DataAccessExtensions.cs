using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Extensions.ServiceRegistration;

public static class DataAccessExtensions
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        RunTimeEnvironment environment)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("DataAccessConfiguration");

        if (environment == RunTimeEnvironment.Production)
        {
            AddProductionDataAccess(services, configuration, logger);
        }
        else
        {
            AddDevelopmentDataAccess(services, configuration, logger);
        }

        return services;
    }

    private static void AddProductionDataAccess(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        services.AddOptions<ApplicationInsightsSettings>()
            .Bind(configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddApplicationInsightsTelemetry();

        services.AddOptions<CosmosDbSettings>()
            .Bind(configuration.GetSection(AppConstants.CosmosDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        logger.LogInformation("Configuring Cosmos DB repository for production");
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
        services.AddSingleton<ITransactionQueryService, CosmosDbTransactionQueryService>();
        services.AddSingleton<ITransactionRepository, CosmosTransactionRepository>();
        services.AddSingleton<IAnalyticsRepository, CosmosDbAnalyticsRepository>();
    }

    private static void AddDevelopmentDataAccess(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Testing")
        {
            services.AddOptions<ApplicationInsightsSettings>()
                .Bind(configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddApplicationInsightsTelemetry();
        }

        services.AddOptions<MongoDbSettings>()
            .Bind(configuration.GetSection(AppConstants.MongoDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        logger.LogInformation("Configuring MongoDB repository for local development/testing");
        services.AddSingleton<ITransactionRepository, MongoTransactionRepository>();
        services.AddSingleton<IAnalyticsRepository, MongoAnalyticsRepository>();
    }
}
