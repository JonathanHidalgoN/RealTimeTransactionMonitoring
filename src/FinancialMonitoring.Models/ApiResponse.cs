using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models;

/// <summary>
/// Standard API response envelope for consistent response format
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = AppConstants.ApiVersion;

    public static ApiResponse<T> SuccessResponse(T data, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            CorrelationId = correlationId
        };
    }
}

/// <summary>
/// API response for error scenarios using RFC 7807 Problem Details
/// </summary>
public class ApiErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = false;

    [JsonPropertyName("error")]
    public ProblemDetails Error { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = AppConstants.ApiVersion;

    public static ApiErrorResponse FromProblemDetails(ProblemDetails problem, string? correlationId = null)
    {
        return new ApiErrorResponse
        {
            Error = problem,
            CorrelationId = correlationId
        };
    }
}

/// <summary>
/// RFC 7807 Problem Details for structured error responses
/// </summary>
public class ProblemDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "about:blank";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("extensions")]
    public Dictionary<string, object>? Extensions { get; set; }

    public static ProblemDetails ValidationError(string detail, string? instance = null)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Detail = detail,
            Status = 400,
            Instance = instance
        };
    }

    public static ProblemDetails NotFound(string detail, string? instance = null)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Resource Not Found",
            Detail = detail,
            Status = 404,
            Instance = instance
        };
    }

    public static ProblemDetails InternalServerError(string detail, string? instance = null)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Detail = detail,
            Status = 500,
            Instance = instance
        };
    }

    public static ProblemDetails Unauthorized(string detail, string? instance = null)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Detail = detail,
            Status = 401,
            Instance = instance
        };
    }

    public static ProblemDetails TooManyRequests(string detail, string? instance = null)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Title = "Too Many Requests",
            Detail = detail,
            Status = 429,
            Instance = instance
        };
    }
}
