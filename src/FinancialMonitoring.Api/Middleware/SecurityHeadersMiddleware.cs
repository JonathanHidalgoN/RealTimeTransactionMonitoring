using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Middleware;

/// <summary>
/// Middleware to add security headers for production hardening
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context.Response);

        // Remove server information disclosure headers before response starts
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }

    private static void AddSecurityHeaders(HttpResponse response)
    {
        // clickjacking attacks
        response.Headers.TryAdd("X-Frame-Options", "DENY");

        //MIME type sniffing
        response.Headers.TryAdd("X-Content-Type-Options", "nosniff");

        // XSS protection
        response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");

        // Content Security Policy for API
        response.Headers.TryAdd("Content-Security-Policy",
            "default-src 'none'; " +
            "script-src 'none'; " +
            "object-src 'none'; " +
            "style-src 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");

        // Referrer Policy
        response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy (formerly Feature Policy) - control browser features
        response.Headers.TryAdd("Permissions-Policy",
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "payment=(), " +
            "usb=(), " +
            "accelerometer=(), " +
            "gyroscope=()");

        // Note: Only add HSTS in production with HTTPS
        if (IsHttpsRequest(response.HttpContext))
        {
            response.Headers.TryAdd("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        // Add API-specific security headers
        response.Headers.TryAdd("X-API-Version", AppConstants.ApiVersion);
        response.Headers.TryAdd("X-Rate-Limit-Policy", "Enabled");
    }

    private static bool IsHttpsRequest(HttpContext context)
    {
        return context.Request.IsHttps ||
               context.Request.Headers.ContainsKey("X-Forwarded-Proto") &&
               context.Request.Headers["X-Forwarded-Proto"].ToString().Contains("https");
    }
}
