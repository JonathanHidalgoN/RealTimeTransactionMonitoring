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

        // Register AuthService as singleton (needs to persist tokens across components)
        builder.Services.AddSingleton<AuthService>();

        // Configure HttpClient for API calls (JWT token added per-request in ApiClientService)
        builder.Services.AddHttpClient<ApiClientService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
            // Note: Authorization header is set per-request in ApiClientService using AuthService
        });

        builder.Services.AddMudServices();

        await builder.Build().RunAsync();
    }
}
