using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using TransactionProcessor.Messaging;

namespace TransactionProcessor.Extensions.ServiceRegistration;

public static class DevelopmentServicesExtensions
{
    public static IServiceCollection AddDevelopmentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoDbSettings>()
            .Bind(configuration.GetSection(AppConstants.MongoDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<KafkaSettings>()
            .Bind(configuration.GetSection(AppConstants.KafkaConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMessageConsumer<object?, string>, KafkaConsumer>();
        services.AddScoped<ITransactionRepository, MongoTransactionRepository>();
        services.AddSingleton<IAnomalyEventPublisher, NoOpAnomalyEventPublisher>();

        Console.WriteLine("Configured development services: MongoDB, Kafka, NoOp publisher");

        return services;
    }
}