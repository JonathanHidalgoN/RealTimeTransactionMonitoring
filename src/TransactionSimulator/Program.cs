using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionSimulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddApplicationInsightsTelemetryWorkerService();

        builder.Services.AddHostedService<Simulator>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
