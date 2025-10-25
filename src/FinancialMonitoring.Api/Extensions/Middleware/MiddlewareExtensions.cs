using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using FinancialMonitoring.Api.Middleware;
using System.Text.Json;

namespace FinancialMonitoring.Api.Extensions.Middleware;

public static class MiddlewareExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        if (app.Environment.IsProduction())
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

        app.ConfigureHealthChecks();
        app.MapControllers();

        return app;
    }

    private static void ConfigureHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
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
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        app.MapHealthChecks("/healthz");
    }
}
