using AspNetCoreRateLimit;
using Azure.Identity;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Extensions;
using FinancialMonitoring.Api.Middleware;
using FinancialMonitoring.Api.HealthChecks;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public partial class Program
{
    public static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Program>();

        var builder = WebApplication.CreateBuilder(args);
        RunTimeEnvironment runTimeEnv = DetectAndConfigureEnvironment(builder, logger);

        ConfigureCommonServices(builder, logger);

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            ConfigureProductionServices(builder, logger);
        }
        else
        {
            ConfigureDevelopmentServices(builder, logger);
        }

        var app = builder.Build();
        ConfigureMiddleware(app);

        app.Run();
    }

    /// <summary>
    /// Detects the runtime environment and configures Azure Key Vault for Production
    /// </summary>
    private static RunTimeEnvironment DetectAndConfigureEnvironment(WebApplicationBuilder builder, ILogger logger)
    {
        var environmentString = builder.Configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var runTimeEnv = RunTimeEnvironmentExtensions.FromString(environmentString);

        logger.LogInformation("Running API program in environment: {Environment}", runTimeEnv);

        if (runTimeEnv == RunTimeEnvironment.Production)
        {
            var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];

            if (!string.IsNullOrEmpty(keyVaultUri) && Uri.TryCreate(keyVaultUri, UriKind.Absolute, out var vaultUri))
            {
                logger.LogInformation("Attempting to load configuration from Azure Key Vault: {KeyVaultUri}", vaultUri);
                try
                {
                    builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());
                    logger.LogInformation("Successfully configured to load secrets from Azure Key Vault");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error connecting to Azure Key Vault: {KeyVaultUri}", vaultUri);
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
    private static void ConfigureCommonServices(WebApplicationBuilder builder, ILogger logger)
    {
        var portSettings = builder.Configuration.BuildPortSettings();

        builder.Services.Configure<PortSettings>(options =>
        {
            options.Api = portSettings.Api;
            options.BlazorHttp = portSettings.BlazorHttp;
            options.BlazorHttps = portSettings.BlazorHttps;
            options.MongoDb = portSettings.MongoDb;
        });

        logger.LogInformation("Port Configuration: API={ApiPort} (from {ApiPortSource}), Blazor HTTP={BlazorHttpPort} (from {BlazorHttpSource}), Blazor HTTPS={BlazorHttpsPort} (from {BlazorHttpsSource}), MongoDB={MongoDbPort} (from {MongoDbSource})",
            portSettings.Api, builder.Configuration["API_PORT"] != null ? "environment" : "default",
            portSettings.BlazorHttp, builder.Configuration["BLAZOR_HTTP_PORT"] != null ? "environment" : "default",
            portSettings.BlazorHttps, builder.Configuration["BLAZOR_HTTPS_PORT"] != null ? "environment" : "default",
            portSettings.MongoDb, builder.Configuration["MONGODB_PORT"] != null ? "environment" : "default");

        ConfigureCors(builder, portSettings, logger);

        ConfigureCaching(builder, logger);

        ConfigureRateLimiting(builder, logger);

        ConfigureApiServices(builder, logger);
        builder.Services.AddHealthChecks()
            .AddCheck<ApiHealthCheck>(AppConstants.ApiHealthCheckName)
            .AddCheck<DatabaseHealthCheck>(AppConstants.DatabaseHealthCheckName);

        builder.Services.AddOptions<ApiSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.ApiSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<CacheSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.CacheSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<ResponseCacheSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.ResponseCacheSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<RateLimitSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.RateLimitSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection(AppConstants.JwtSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddControllers();

        builder.Services.AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddSingleton<IPasswordHashingService, PasswordHashingService>();

        builder.Services.AddSingleton<IOAuthClientRepository, InMemoryOAuthClientRepository>();
        builder.Services.AddScoped<IOAuthClientService, OAuthClientService>();

        ConfigureSwagger(builder);
    }

    /// <summary>
    /// Configures CORS settings
    /// </summary>
    private static void ConfigureCors(WebApplicationBuilder builder, PortSettings portSettings, ILogger logger)
    {
        builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(AppConstants.CorsConfigPrefix));

        var corsSettings = new CorsSettings();
        builder.Configuration.GetSection(AppConstants.CorsConfigPrefix).Bind(corsSettings);

        var allowedOrigins = corsSettings.AllowedOrigins.Length > 0
            ? corsSettings.AllowedOrigins
            : CorsSettings.BuildDefaultOrigins(portSettings);

        logger.LogInformation("CORS Policy: Allowing origins: {AllowedOrigins}", string.Join(", ", allowedOrigins));

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
    private static void ConfigureCaching(WebApplicationBuilder builder, ILogger logger)
    {
        var cacheSettings = builder.Configuration.GetSection(AppConstants.CacheSettingsConfigPrefix).Get<CacheSettings>() ?? new CacheSettings();
        var responseCacheSettings = builder.Configuration.GetSection(AppConstants.ResponseCacheSettingsConfigPrefix).Get<ResponseCacheSettings>() ?? new ResponseCacheSettings();

        builder.Services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = responseCacheSettings.MaximumBodySizeBytes;
            options.UseCaseSensitivePaths = responseCacheSettings.UseCaseSensitivePaths;
            options.SizeLimit = responseCacheSettings.SizeLimitBytes;
        });

        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.ConditionalExpire(cacheSettings.BasePolicyCacheSeconds));

            options.AddPolicy(AppConstants.TransactionCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.TransactionCacheSeconds,
                    b => b.SetVaryByQuery("pageNumber", "pageSize", "startDate", "endDate", "minAmount", "maxAmount")));

            options.AddPolicy(AppConstants.TransactionByIdCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.TransactionByIdCacheSeconds,
                    b => b.SetVaryByRouteValue("id")));

            options.AddPolicy(AppConstants.AnomalousTransactionCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.AnomalousTransactionCacheSeconds,
                    b => b.SetVaryByQuery("pageNumber", "pageSize")));

            options.AddPolicy(AppConstants.AnalyticsCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.AnalyticsCacheSeconds));

            options.AddPolicy(AppConstants.TimeSeriesCachePolicy, builder =>
                builder.ConditionalExpire(cacheSettings.TimeSeriesCacheSeconds,
                    b => b.SetVaryByQuery("hours", "intervalMinutes")));
        });

        logger.LogInformation("Cache Configuration: Response Cache={ResponseCacheMaxMB}MB max body, {ResponseCacheTotalMB}MB total, Transaction Cache={TransactionCache}, Transaction By ID Cache={TransactionByIdCache}, Anomalous Transaction Cache={AnomalousCache}, Analytics Cache={AnalyticsCache}, Time Series Cache={TimeSeriesCache}",
            responseCacheSettings.MaximumBodySizeMB, responseCacheSettings.SizeLimitMB,
            cacheSettings.TransactionCacheSeconds > 0 ? $"{cacheSettings.TransactionCacheSeconds}s" : "disabled",
            cacheSettings.TransactionByIdCacheSeconds > 0 ? $"{cacheSettings.TransactionByIdCacheSeconds}s" : "disabled",
            cacheSettings.AnomalousTransactionCacheSeconds > 0 ? $"{cacheSettings.AnomalousTransactionCacheSeconds}s" : "disabled",
            cacheSettings.AnalyticsCacheSeconds > 0 ? $"{cacheSettings.AnalyticsCacheSeconds}s" : "disabled",
            cacheSettings.TimeSeriesCacheSeconds > 0 ? $"{cacheSettings.TimeSeriesCacheSeconds}s" : "disabled");
    }

    /// <summary>
    /// Configures rate limiting with configurable settings
    /// </summary>
    private static void ConfigureRateLimiting(WebApplicationBuilder builder, ILogger logger)
    {
        var rateLimitSettings = builder.Configuration.GetSection(AppConstants.RateLimitSettingsConfigPrefix).Get<RateLimitSettings>() ?? new RateLimitSettings();

        builder.Services.AddMemoryCache();
        builder.Services.AddInMemoryRateLimiting();
        builder.Services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = rateLimitSettings.EnableEndpointRateLimiting;
            options.StackBlockedRequests = rateLimitSettings.StackBlockedRequests;
            options.HttpStatusCode = rateLimitSettings.HttpStatusCode;
            options.RealIpHeader = rateLimitSettings.RealIpHeader;
            options.ClientIdHeader = rateLimitSettings.ClientIdHeader;

            // Convert our settings to AspNetCoreRateLimit's RateLimitRule format
            options.GeneralRules = rateLimitSettings.GeneralRules.Select(rule => new RateLimitRule
            {
                Endpoint = rule.Endpoint,
                Period = rule.Period,
                Limit = rule.Limit
            }).ToList();
        });
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        logger.LogInformation("Rate Limiting Configuration: Endpoint Rate Limiting={EndpointRateLimiting}, Stack Blocked Requests={StackBlockedRequests}, HTTP Status Code={HttpStatusCode}, Rules={RuleCount}",
            rateLimitSettings.EnableEndpointRateLimiting ? "enabled" : "disabled",
            rateLimitSettings.StackBlockedRequests,
            rateLimitSettings.HttpStatusCode,
            rateLimitSettings.GeneralRules.Count);

        foreach (var rule in rateLimitSettings.GeneralRules)
        {
            logger.LogDebug("Rate Limit Rule: {Endpoint} - {Limit} requests per {Period}",
                rule.Endpoint, rule.Limit, rule.Period);
        }
    }

    /// <summary>
    /// Configures API versioning and authentication
    /// </summary>
    private static void ConfigureApiServices(WebApplicationBuilder builder, ILogger logger)
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

        var jwtSettings = builder.Configuration.GetSection(AppConstants.JwtSettingsConfigPrefix).Get<JwtSettings>() ?? new JwtSettings();

        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, SecureApiKeyAuthenticationHandler>(
                SecureApiKeyAuthenticationDefaults.SchemeName,
                options => { })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtSettings.ValidateIssuer,
                    ValidateAudience = jwtSettings.ValidateAudience,
                    ValidateLifetime = jwtSettings.ValidateLifetime,
                    ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        logger.LogWarning("JWT Authentication failed: {ErrorMessage}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        logger.LogDebug("JWT Token validated for user: {UserName}", context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AppConstants.AdminRole, policy => policy.RequireRole(AppConstants.AdminRole));
            options.AddPolicy(AppConstants.ViewerRole, policy => policy.RequireRole(AppConstants.ViewerRole));
            options.AddPolicy(AppConstants.AnalystRole, policy => policy.RequireRole(AppConstants.AnalystRole));
        });
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

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Format: Authorization: Bearer {token}",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
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

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "bearer",
                        Name = "Bearer",
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

        });
    }

    /// <summary>
    /// Configures services for Production environment (Azure CosmosDB + Application Insights)
    /// </summary>
    private static void ConfigureProductionServices(WebApplicationBuilder builder, ILogger logger)
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

        logger.LogInformation("Configuring Cosmos DB repository for production");
        builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
        builder.Services.AddSingleton<ITransactionQueryService, CosmosDbTransactionQueryService>();
        builder.Services.AddSingleton<ITransactionRepository, CosmosTransactionRepository>();
        builder.Services.AddSingleton<IAnalyticsRepository, CosmosDbAnalyticsRepository>();
    }

    /// <summary>
    /// Configures services for Development/Testing environment (MongoDB)
    /// </summary>
    private static void ConfigureDevelopmentServices(WebApplicationBuilder builder, ILogger logger)
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

        logger.LogInformation("Configuring MongoDB repository for local development/testing");
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

        app.UseResponseCaching();
        app.UseOutputCache();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseCors("_myAllowSpecificOrigins");
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
