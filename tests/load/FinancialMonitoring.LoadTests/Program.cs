using NBomber.CSharp;
using FinancialMonitoring.Models;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var apiBaseUrl = config["ApiBaseUrl"] ?? "http://localhost:5100";
var apiKey = config["ApiKey"] ?? "your-api-key";

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
httpClient.Timeout = TimeSpan.FromSeconds(15);

var scenario = Scenario.Create("api_load_test", async context =>
{
    var pageNumber = Random.Shared.Next(1, 6);
    var pageSize = Random.Shared.Next(5, 21);

    var response = await httpClient.GetAsync($"{apiBaseUrl}/api/transactions?pageNumber={pageNumber}&pageSize={pageSize}");

    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 2, interval: TimeSpan.FromSeconds(2), during: TimeSpan.FromSeconds(60))
);

var kafkaScenario = Scenario.Create("anomaly_detection_test", async context =>
{
    var pageNumber = Random.Shared.Next(1, 3);
    var pageSize = Random.Shared.Next(10, 51);

    var response = await httpClient.GetAsync($"{apiBaseUrl}/api/transactions/anomalies?pageNumber={pageNumber}&pageSize={pageSize}");

    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 1, interval: TimeSpan.FromSeconds(3), during: TimeSpan.FromSeconds(45))
);

var memoryScenario = Scenario.Create("memory_intensive_test", async context =>
{
    var response = await httpClient.GetAsync($"{apiBaseUrl}/api/transactions?pageSize=100");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var transactions = JsonSerializer.Deserialize<PagedResult<Transaction>>(content);

        var processedTransactions = transactions?.Items?
            .Where(t => t.Amount > 100)
            .OrderByDescending(t => t.Amount)
            .Take(10)
            .ToList();

        return Response.Ok();
    }

    return Response.Fail();
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 1, interval: TimeSpan.FromSeconds(3), during: TimeSpan.FromSeconds(40))
);

var transactionByIdScenario = Scenario.Create("transaction_lookup_test", async context =>
{
    var randomId = Guid.NewGuid().ToString();

    var response = await httpClient.GetAsync($"{apiBaseUrl}/api/transactions/{randomId}");

    return (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
        ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 1, interval: TimeSpan.FromSeconds(4), during: TimeSpan.FromSeconds(35))
);

Console.WriteLine("Starting Load Tests...");
Console.WriteLine($"API Base URL: {apiBaseUrl}");
Console.WriteLine($"Test Duration: ~2 minutes");
Console.WriteLine("Press Ctrl+C to stop");

NBomberRunner
    .RegisterScenarios(scenario, kafkaScenario, memoryScenario, transactionByIdScenario)
    .WithReportFolder("load-test-reports")
    .Run();

Console.WriteLine("Load tests completed. Check the 'load-test-reports' folder for detailed results.");
