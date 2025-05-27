using FinancialMonitoring.Models;
using TransactionSimulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Transaction Simulator Application starting...");
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable(AppConstants.KafkaBootstrapServersEnvVarName);

        if (string.IsNullOrEmpty(kafkaBootstrapServers))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"CRITICAL: Environment variable '{AppConstants.KafkaBootstrapServersEnvVarName}' is not set. This app needs it to connect to Kafka.");
            return;
        }

        Simulator simulator = new Simulator(kafkaBootstrapServers);
        //https://learn.microsoft.com/es-es/dotnet/api/system.threading.cancellationtokensource?view=net-8.0
        CancellationTokenSource cts = new CancellationTokenSource();
        //https://learn.microsoft.com/en-us/dotnet/api/system.console.cancelkeypress?view=net-8.0
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Console.WriteLine("Ctrl+C detected. Initiating shutdown...");
            cts.Cancel();
            //Tell OS we are closing, don't kill us
            eventArgs.Cancel = true;
        };
        //Occurs when the default application domain's parent process exits.
        AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
        {
            Console.WriteLine("Process exit detected. Initiating shutdown...");
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        };

        try
        {
            await simulator.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Simulator operation was canceled.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An unexpected error occurred in the simulator: {ex}");
            Console.ResetColor();
        }
        finally
        {
            cts.Dispose();
            Console.WriteLine("Transaction Simulator Application finished.");
        }

    }
}