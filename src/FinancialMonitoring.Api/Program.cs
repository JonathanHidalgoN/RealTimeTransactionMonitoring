using Azure.Identity;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection(AppConstants.CosmosDbConfigPrefix));

var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];
if (!string.IsNullOrEmpty(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var vaultUri))
{
    Console.WriteLine($"Attempting to load configuration from Azure Key Vault: {vaultUri}");
    try
    {
        builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
        Console.WriteLine("Successfully configured to load secrets from Azure Key Vault.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error connecting to Azure Key Vault: {ex.Message}");
    }
}
else
{
    Console.WriteLine("KEY_VAULT_URI not configured. Key Vault secrets will not be loaded.");
}
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddSingleton<ITransactionQueryService, CosmosDbTransactionQueryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
