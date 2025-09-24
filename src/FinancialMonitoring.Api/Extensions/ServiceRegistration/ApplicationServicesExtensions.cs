using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Api.HealthChecks;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Extensions;

namespace FinancialMonitoring.Api.Extensions.ServiceRegistration;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiSettings>()
            .Bind(configuration.GetSection(AppConstants.ApiSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var portSettings = configuration.BuildPortSettings();
        services.Configure<PortSettings>(options =>
        {
            options.Api = portSettings.Api;
            options.BlazorHttp = portSettings.BlazorHttp;
            options.BlazorHttps = portSettings.BlazorHttps;
            options.MongoDb = portSettings.MongoDb;
        });

        services.AddControllers();

        services.AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddEndpointsApiExplorer();

        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Version")
            );
        });

        services.AddHealthChecks()
            .AddCheck<ApiHealthCheck>(AppConstants.ApiHealthCheckName)
            .AddCheck<DatabaseHealthCheck>(AppConstants.DatabaseHealthCheckName);

        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();

        services.AddSingleton<IOAuthClientRepository, InMemoryOAuthClientRepository>();
        services.AddScoped<IOAuthClientService, OAuthClientService>();

        return services;
    }
}
