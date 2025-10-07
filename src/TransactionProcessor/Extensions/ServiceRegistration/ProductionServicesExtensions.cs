using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using TransactionProcessor.Messaging;

namespace TransactionProcessor.Extensions.ServiceRegistration;

public static class ProductionServicesExtensions
{
    public static IServiceCollection AddProductionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApplicationInsightsSettings>()
            .Bind(configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CosmosDbSettings>()
            .Bind(configuration.GetSection(AppConstants.CosmosDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EventHubsSettings>()
            .Bind(configuration.GetSection(AppConstants.EventHubsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMessageConsumer<object?, string>, EventHubsConsumer>();
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
        services.AddSingleton<ITransactionRepository, CosmosTransactionRepository>();
        services.AddSingleton<IAnomalyEventPublisher, EventHubsAnomalyEventPublisher>();
        services.AddApplicationInsightsTelemetryWorkerService();

        Console.WriteLine("Configured production services: CosmosDB, EventHubs, Application Insights");

        return services;
    }
}
