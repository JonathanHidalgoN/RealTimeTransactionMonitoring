using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using TransactionSimulator.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace TransactionSimulator.Extensions.ServiceRegistration;

public static class ProductionServicesExtensions
{
    public static IServiceCollection AddProductionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MessagingSettings>()
            .Bind(configuration.GetSection(AppConstants.EventHubsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ApplicationInsightsSettings>()
            .Bind(configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMessageProducer<Null, string>, EventHubsProducer>();
        services.AddApplicationInsightsTelemetryWorkerService();

        Console.WriteLine("Configured production services: EventHubs, Application Insights");

        return services;
    }
}
