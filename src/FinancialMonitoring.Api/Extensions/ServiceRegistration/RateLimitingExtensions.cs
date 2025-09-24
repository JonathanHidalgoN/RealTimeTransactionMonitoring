using AspNetCoreRateLimit;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Extensions.ServiceRegistration;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("RateLimitingConfiguration");

        var rateLimitSettings = configuration.GetSection(AppConstants.RateLimitSettingsConfigPrefix).Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddOptions<RateLimitSettings>()
            .Bind(configuration.GetSection(AppConstants.RateLimitSettingsConfigPrefix))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddInMemoryRateLimiting();
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = rateLimitSettings.EnableEndpointRateLimiting;
            options.StackBlockedRequests = rateLimitSettings.StackBlockedRequests;
            options.HttpStatusCode = rateLimitSettings.HttpStatusCode;
            options.RealIpHeader = rateLimitSettings.RealIpHeader;
            options.ClientIdHeader = rateLimitSettings.ClientIdHeader;

            options.GeneralRules = rateLimitSettings.GeneralRules.Select(rule => new RateLimitRule
            {
                Endpoint = rule.Endpoint,
                Period = rule.Period,
                Limit = rule.Limit
            }).ToList();
        });
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

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

        return services;
    }
}
