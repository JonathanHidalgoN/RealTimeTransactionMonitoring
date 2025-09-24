using FinancialMonitoring.Abstractions.Messaging;
using FinancialMonitoring.Models;
using TransactionSimulator.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace TransactionSimulator.Extensions.ServiceRegistration;

public static class DevelopmentServicesExtensions
{
    public static IServiceCollection AddDevelopmentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KafkaSettings>()
            .Bind(configuration.GetSection(AppConstants.KafkaConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMessageProducer<Null, string>, KafkaProducer>();

        Console.WriteLine("Configured development services: Kafka");

        return services;
    }
}
