using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Middleware;

/// <summary>
/// Middleware to ensure every request has a correlation ID for distributed tracing
/// </summary>
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-9.0
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-9.0
public class CorrelationIdMiddleware
{
    private static readonly string CorrelationIdHeaderName = AppConstants.CorrelationIdHeader;
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;

        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue))
        {
            correlationId = headerValue.FirstOrDefault() ?? context.TraceIdentifier;
        }
        else
        {
            correlationId = context.TraceIdentifier;
        }

        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        //https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-9.0
        //scopes to add this info to all logs:[CorrelationId:abc-123] [Path:/api/transactions] [Method:GET] Processing request
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method
        });

        _logger.LogInformation("Processing request {Method} {Path} with correlation ID {CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);

        await _next(context);

        _logger.LogInformation("Completed request {Method} {Path} with status {StatusCode} for correlation ID {CorrelationId}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, correlationId);
    }
}
