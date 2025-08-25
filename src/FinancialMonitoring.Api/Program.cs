using AspNetCoreRateLimit;
using Azure.Identity;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Middleware;
using FinancialMonitoring.Api.HealthChecks;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Api.Swagger;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Caching.Memory;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        RunTimeEnvironment runTimeEnv = DetectAndConfigureEnvironment(builder);

        ConfigureCommonServices(builder);

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            ConfigureProductionServices(builder);
        }
        else
        {
            ConfigureDevelopmentServices(builder);
        }

        var app = builder.Build();
        ConfigureMiddleware(app);

        app.Run();
    }

    /// <summary>
    /// Detects the runtime environment and configures Azure Key Vault for Production
    /// </summary>
    private static RunTimeEnvironment DetectAndConfigureEnvironment(WebApplicationBuilder builder)
    {
        var environmentString = builder.Configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var runTimeEnv = RunTimeEnvironmentExtensions.FromString(environmentString);

        Console.WriteLine($"Running API program in environment: {runTimeEnv}");

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
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
                    throw;
                }
            }
            else
            {
                throw new ArgumentException("KEY_VAULT_URI environment variable is required for Production runtime");
            }
        }

        return runTimeEnv;
    }

    /// <summary>
    /// Configures common services used by all environments
    /// </summary>
    private static void ConfigureCommonServices(WebApplicationBuilder builder)
    {
        var portSettings = builder.Configuration.BuildPortSettings();

        builder.Services.Configure<PortSettings>(options =>
        {
            options.Api = portSettings.Api;
            options.BlazorHttp = portSettings.BlazorHttp;
            options.BlazorHttps = portSettings.BlazorHttps;
            options.MongoDb = portSettings.MongoDb;
        });

        Console.WriteLine($"Port Configuration:");
        Console.WriteLine($"  API Port: {portSettings.Api} (from {(builder.Configuration["API_PORT"] != null ? "environment" : "default")})");
        Console.WriteLine($"  Blazor HTTP Port: {portSettings.BlazorHttp} (from {(builder.Configuration["BLAZOR_HTTP_PORT"] != null ? "environment" : "default")})");
        Console.WriteLine($"  Blazor HTTPS Port: {portSettings.BlazorHttps} (from {(builder.Configuration["BLAZOR_HTTPS_PORT"] != null ? "environment" : "default")})");
        Console.WriteLine($"  MongoDB Port: {portSettings.MongoDb} (from {(builder.Configuration["MONGODB_PORT"] != null ? "environment" : "default")})");

        ConfigureCors(builder, portSettings);

        ConfigureCaching(builder);

        ConfigureRateLimiting(builder);

        ConfigureApiServices(builder);
        builder.Services.AddHealthChecks()
            .AddCheck<ApiHealthCheck>("api")
            .AddCheck<DatabaseHealthCheck>("database");

        builder.Services.AddOptions<ApiSettings>()
            .Bind(builder.Configuration.GetSection("ApiSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddControllers();

        builder.Services.AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddEndpointsApiExplorer();

        ConfigureSwagger(builder);
    }

    /// <summary>
    /// Configures CORS settings
    /// </summary>
    private static void ConfigureCors(WebApplicationBuilder builder, PortSettings portSettings)
    {
        builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection("Cors"));

        var corsSettings = new CorsSettings();
        builder.Configuration.GetSection("Cors").Bind(corsSettings);

        var allowedOrigins = corsSettings.AllowedOrigins.Length > 0
            ? corsSettings.AllowedOrigins
            : CorsSettings.BuildDefaultOrigins(portSettings);

        Console.WriteLine($"CORS Policy: Allowing origins: {string.Join(", ", allowedOrigins)}");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: "_myAllowSpecificOrigins",
                              policy =>
                              {
                                  policy.WithOrigins(allowedOrigins)
                                        .WithHeaders(corsSettings.AllowedHeaders)
                                        .WithMethods(corsSettings.AllowedMethods);

                                  if (corsSettings.AllowCredentials)
                                  {
                                      policy.AllowCredentials();
                                  }
                              });
        });
    }

    /// <summary>
    /// Configures caching and performance settings
    /// </summary>
    private static void ConfigureCaching(WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 1024 * 1024; // 1MB
            options.UseCaseSensitivePaths = false;
            options.SizeLimit = 10 * 1024 * 1024; // 10MB cache size
        });

        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));

            options.AddPolicy("TransactionCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(2))
                       .SetVaryByQuery("pageNumber", "pageSize", "startDate", "endDate", "minAmount", "maxAmount"));

            options.AddPolicy("TransactionByIdCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(10))
                       .SetVaryByRouteValue("id"));

            options.AddPolicy("AnomalousTransactionCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(1))
                       .SetVaryByQuery("pageNumber", "pageSize"));

            options.AddPolicy("AnalyticsCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(5)));

            options.AddPolicy("TimeSeriesCache", builder =>
                builder.Expire(TimeSpan.FromMinutes(2))
                       .SetVaryByQuery("hours", "intervalMinutes"));
        });
    }

    /// <summary>
    /// Configures rate limiting
    /// </summary>
    private static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
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
    }

    /// <summary>
    /// Configures API versioning and authentication
    /// </summary>
    private static void ConfigureApiServices(WebApplicationBuilder builder)
    {
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
    }

    /// <summary>
    /// Configures Swagger documentation
    /// </summary>
    private static void ConfigureSwagger(WebApplicationBuilder builder)
    {
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

            c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "API Key needed to access the endpoints. Format: X-Api-Key: {your-api-key}",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "X-Api-Key",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });

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

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.DocumentFilter<RateLimitDocumentFilter>();

            c.OperationFilter<CorrelationIdOperationFilter>();
        });
    }

    /// <summary>
    /// Configures services for Production environment (Azure CosmosDB + Application Insights)
    /// </summary>
    private static void ConfigureProductionServices(WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ApplicationInsightsSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddApplicationInsightsTelemetry();

        builder.Services.AddOptions<CosmosDbSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.CosmosDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        Console.WriteLine("Configuring Cosmos DB repository for production");
        builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
        builder.Services.AddSingleton<ITransactionQueryService, CosmosDbTransactionQueryService>();
        builder.Services.AddSingleton<ITransactionRepository, CosmosTransactionRepository>();
        builder.Services.AddSingleton<IAnalyticsRepository, CosmosDbAnalyticsRepository>();
    }

    /// <summary>
    /// Configures services for Development/Testing environment (MongoDB)
    /// </summary>
    private static void ConfigureDevelopmentServices(WebApplicationBuilder builder)
    {
        if (builder.Environment.EnvironmentName != "Testing")
        {
            builder.Services.AddOptions<ApplicationInsightsSettings>()
                .Bind(builder.Configuration.GetSection(AppConstants.ApplicationInsightsConfigPrefix))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            builder.Services.AddApplicationInsightsTelemetry();
        }

        builder.Services.AddOptions<MongoDbSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.MongoDbConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        Console.WriteLine("Configuring MongoDB repository for local development/testing");
        builder.Services.AddSingleton<ITransactionRepository, MongoTransactionRepository>();
        builder.Services.AddSingleton<IAnalyticsRepository, MongoAnalyticsRepository>();
    }

    /// <summary>
    /// Configures middleware pipeline
    /// </summary>
    private static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("_myAllowSpecificOrigins");
        app.UseResponseCaching();
        app.UseOutputCache();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseIpRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();

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

        app.MapHealthChecks("/healthz");

        app.MapControllers();
    }
}

public partial class Program { }
