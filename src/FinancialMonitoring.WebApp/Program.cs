using FinancialMonitoring.WebApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Net.Http;
using FinancialMonitoring.WebApp.Services;
using System.Globalization;

public class Program
{
    public static async Task Main(string[] args)
    {
        var culture = new CultureInfo("es-MX");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Configuration.AddJsonFile("appsettings.json");
        builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Configure base HttpClient with API URL
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
            ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing");

        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        });

        // Register AuthService as scoped (tokens are persisted in localStorage, not in the service)
        builder.Services.AddScoped<AuthService>();

        // Configure HttpClient for API calls (JWT token added per-request in ApiClientService)
        builder.Services.AddHttpClient<ApiClientService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            // Note: Authorization header is set per-request in ApiClientService using AuthService
        });

        builder.Services.AddMudServices();

        await builder.Build().RunAsync();
    }
}
