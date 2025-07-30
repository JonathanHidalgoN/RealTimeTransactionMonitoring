using AspNetCoreRateLimit;
using Azure.Identity;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Middleware;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

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

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5124", "https://localhost:7258" };

Console.WriteLine($"CORS Policy: Allowing origins: {string.Join(", ", allowedOrigins)}");

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins(allowedOrigins)
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.SchemeName,
        options => { });

builder.Services.AddAuthorization();
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddOptions<ApplicationInsightsSettings>()
        .Bind(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
        .ValidateDataAnnotations()
        .ValidateOnStart();
}
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddOptions<MongoDbSettings>()
        .Bind(builder.Configuration.GetSection(AppConstants.MongoDbConfigPrefix))
        .ValidateDataAnnotations()
        .ValidateOnStart();
}
else
{
    builder.Services.AddOptions<CosmosDbSettings>()
        .Bind(builder.Configuration.GetSection(AppConstants.CosmosDbConfigPrefix))
        .ValidateDataAnnotations()
        .ValidateOnStart();
}
builder.Services.AddOptions<ApiSettings>()
    .Bind(builder.Configuration.GetSection("ApiSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddApplicationInsightsTelemetry();
}
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
{
    Console.WriteLine("Configuring MongoDB repository for local development/testing");
    builder.Services.AddSingleton<ITransactionRepository, MongoTransactionRepository>();
}
else
{
    Console.WriteLine("Configuring Cosmos DB repository for production");
    builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
    builder.Services.AddSingleton<ITransactionQueryService, CosmosDbTransactionQueryService>();
    builder.Services.AddSingleton<ITransactionRepository, CosmosTransactionRepository>();
}

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
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/healthz");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
