using System.Net;
using System.Text.Json;
using FinancialMonitoring.Api.Models;

namespace FinancialMonitoring.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-9.0
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-9.0
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    //Wrapper for calls that will catch exceptions 
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        _logger.LogError(exception, 
            "Unhandled exception occurred for request {Method} {Path} with correlation ID {CorrelationId}", 
            context.Request.Method, context.Request.Path, correlationId);

        //This will provide consisten exception handling and error responses
        var problemDetails = exception switch
        {
            ArgumentException => ProblemDetails.ValidationError(
                _environment.IsDevelopment() ? exception.Message : "Invalid request parameters", 
                context.Request.Path),
            
            UnauthorizedAccessException => ProblemDetails.Unauthorized(
                "Authentication failed or insufficient permissions", 
                context.Request.Path),
            
            KeyNotFoundException => ProblemDetails.NotFound(
                "The requested resource was not found", 
                context.Request.Path),
            
            InvalidOperationException => ProblemDetails.ValidationError(
                _environment.IsDevelopment() ? exception.Message : "Invalid operation", 
                context.Request.Path),
            
            _ => ProblemDetails.InternalServerError(
                _environment.IsDevelopment() 
                    ? $"{exception.Message}\n{exception.StackTrace}" 
                    : "An unexpected error occurred",
                context.Request.Path)
        };

        var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);

        context.Response.StatusCode = problemDetails.Status;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}