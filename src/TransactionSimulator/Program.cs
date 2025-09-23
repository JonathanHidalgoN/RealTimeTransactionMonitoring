using TransactionSimulator;
using TransactionSimulator.Extensions.Configuration;
using TransactionSimulator.Extensions.ServiceRegistration;
using TransactionSimulator.Generation;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var environment = EnvironmentDetector.DetectAndConfigureEnvironment(builder);

        ConfigurationValidator.ValidateConfiguration(builder.Configuration, environment);

        builder.Services.AddSingleton<ITransactionGenerator, TransactionGenerator>();
        builder.Services.AddHostedService<Simulator>();

        if (environment == RunTimeEnvironment.Production)
        {
            builder.Services.AddProductionServices(builder.Configuration);
        }
        else
        {
            builder.Services.AddDevelopmentServices(builder.Configuration);
        }

        var host = builder.Build();
        await host.RunAsync();
    }
}
