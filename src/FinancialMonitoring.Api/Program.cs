using AspNetCoreRateLimit;
using Azure.Identity;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Middleware;
using FinancialMonitoring.Api.HealthChecks;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Api.Swagger;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

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

// Rate Limiting
//https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule 
        { 
            Endpoint = "*", 
            Period = "1m", 
            Limit = 1000 
        },
        new RateLimitRule 
        { 
            Endpoint = "*/transactions", 
            Period = "1m", 
            Limit = 100 
        }
    };
});
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// API Versioning
//https://www.hanselman.com/blog/aspnet-core-restful-web-api-versioning-made-easy
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version")
    );
});

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, SecureApiKeyAuthenticationHandler>(
        SecureApiKeyAuthenticationDefaults.SchemeName,
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

builder.Services.AddHealthChecks()
    .AddCheck<ApiHealthCheck>("api")
    .AddCheck<DatabaseHealthCheck>("database");

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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Financial Monitoring API",
        Description = "Enterprise-grade API for real-time financial transaction monitoring and anomaly detection",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Financial Monitoring Team",
            Email = "support@financialmonitoring.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add API Key Security Definition
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. Format: X-Api-Key: {your-api-key}",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    // Add Security Requirement
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Scheme = "ApiKeyScheme",
                Name = "ApiKey",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Document rate limiting
    c.DocumentFilter<RateLimitDocumentFilter>();
    
    // Document correlation IDs
    c.OperationFilter<CorrelationIdOperationFilter>();
});

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

// Middleware Pipeline - Order is critical!
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseCors(MyAllowSpecificOrigins);
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoints
// https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-9.0
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("O"),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                data = entry.Value.Data,
                exception = entry.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Simple health check for load balancers
app.MapHealthChecks("/healthz");

app.MapControllers();

app.Run();

public partial class Program { }
