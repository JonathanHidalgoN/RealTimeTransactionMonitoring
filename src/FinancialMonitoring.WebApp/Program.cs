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

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddHttpClient<ApiClientService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);

            client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["ApiKey"]);
        });

        builder.Services.AddMudServices();

        await builder.Build().RunAsync();
    }
}
