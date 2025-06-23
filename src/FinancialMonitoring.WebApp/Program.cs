using FinancialMonitoring.WebApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Net.Http;
using FinancialMonitoring.WebApp.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Configuration.AddJsonFile("appsettings.json");

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // --- REPLACE the old AddScoped line WITH THIS BLOCK ---

        // Register ApiClientService and configure a typed HttpClient for it.
        // This is the standard "HttpClientFactory" pattern.
        builder.Services.AddHttpClient<ApiClientService>(client =>
        {
            // Set the base address for all requests made by this client
            client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);

            // Add the API Key header to every request made by this client
            client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["ApiKey"]);
        });
        // ----------------------------------------------------

        builder.Services.AddMudServices();

        await builder.Build().RunAsync();
    }
}
