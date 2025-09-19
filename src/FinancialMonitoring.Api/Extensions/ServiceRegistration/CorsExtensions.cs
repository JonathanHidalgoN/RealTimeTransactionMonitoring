using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Extensions.ServiceRegistration;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PortSettings portSettings)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("CorsConfiguration");

        services.Configure<CorsSettings>(configuration.GetSection(AppConstants.CorsConfigPrefix));

        var corsSettings = new CorsSettings();
        configuration.GetSection(AppConstants.CorsConfigPrefix).Bind(corsSettings);

        var allowedOrigins = corsSettings.AllowedOrigins.Length > 0
            ? corsSettings.AllowedOrigins
            : CorsSettings.BuildDefaultOrigins(portSettings);

        logger.LogInformation("CORS Policy: Allowing origins: {AllowedOrigins}", string.Join(", ", allowedOrigins));

        services.AddCors(options =>
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

        return services;
    }
}
