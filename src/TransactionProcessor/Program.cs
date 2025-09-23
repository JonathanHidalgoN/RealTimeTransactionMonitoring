using TransactionProcessor;
using TransactionProcessor.Extensions.Configuration;
using TransactionProcessor.Extensions.ServiceRegistration;
using TransactionProcessor.HostedServices;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var environment = EnvironmentDetector.DetectAndConfigureEnvironment(builder);

        ConfigurationValidator.ValidateConfiguration(builder.Configuration, environment);

        builder.Services.AddHostedService<DatabaseInitializerHostedService>();
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddScoped<ITransactionProcessor, TransactionProcessor.Services.TransactionProcessor>();

        if (environment == RunTimeEnvironment.Production)
        {
            builder.Services.AddProductionServices(builder.Configuration);
        }
        else
        {
            builder.Services.AddDevelopmentServices(builder.Configuration);
        }

        builder.Services.AddAnomalyDetection(builder.Configuration);

        var host = builder.Build();
        host.Run();
    }
}

