using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinancialMonitoring.Api.Swagger;

/// <summary>
/// Swagger operation filter to document correlation ID headers
/// </summary>
public class CorrelationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add optional correlation ID parameter
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional correlation ID for request tracing. If not provided, one will be generated automatically.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = new Microsoft.OpenApi.Any.OpenApiString("abc123-def456-ghi789")
            }
        });

        // Add correlation ID to all response headers
        foreach (var response in operation.Responses.Values)
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();

            response.Headers.TryAdd("X-Correlation-Id", new OpenApiHeader
            {
                Description = "Correlation ID for request tracing and debugging",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "uuid"
                }
            });
        }

        // Add examples for common response scenarios
        if (operation.Responses.ContainsKey("200"))
        {
            var successResponse = operation.Responses["200"];
            if (successResponse.Content?.ContainsKey("application/json") == true)
            {
                var mediaType = successResponse.Content["application/json"];
                if (mediaType.Examples == null)
                {
                    mediaType.Examples = new Dictionary<string, OpenApiExample>();
                }

                mediaType.Examples.TryAdd("success-with-correlation", new OpenApiExample
                {
                    Summary = "Successful response with correlation ID",
                    Description = "Example of a successful API response including correlation ID for tracing",
                    Value = new Microsoft.OpenApi.Any.OpenApiObject
                    {
                        ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                        ["data"] = new Microsoft.OpenApi.Any.OpenApiObject
                        {
                            ["items"] = new Microsoft.OpenApi.Any.OpenApiArray(),
                            ["pageNumber"] = new Microsoft.OpenApi.Any.OpenApiInteger(1),
                            ["pageSize"] = new Microsoft.OpenApi.Any.OpenApiInteger(20),
                            ["totalCount"] = new Microsoft.OpenApi.Any.OpenApiInteger(150),
                            ["totalPages"] = new Microsoft.OpenApi.Any.OpenApiInteger(8),
                            ["hasNextPage"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                            ["hasPreviousPage"] = new Microsoft.OpenApi.Any.OpenApiBoolean(false)
                        },
                        ["timestamp"] = new Microsoft.OpenApi.Any.OpenApiString("2024-01-15T10:30:00Z"),
                        ["correlationId"] = new Microsoft.OpenApi.Any.OpenApiString("abc123-def456-ghi789"),
                        ["version"] = new Microsoft.OpenApi.Any.OpenApiString("1.0")
                    }
                });
            }
        }

        // Add error response examples
        if (operation.Responses.ContainsKey("400") || operation.Responses.ContainsKey("404") || operation.Responses.ContainsKey("500"))
        {
            var errorResponses = operation.Responses.Where(r =>
                r.Key == "400" || r.Key == "404" || r.Key == "500").ToList();

            foreach (var (statusCode, response) in errorResponses)
            {
                if (response.Content?.ContainsKey("application/json") == true)
                {
                    var mediaType = response.Content["application/json"];
                    if (mediaType.Examples == null)
                    {
                        mediaType.Examples = new Dictionary<string, OpenApiExample>();
                    }

                    mediaType.Examples.TryAdd($"error-{statusCode}-with-correlation", new OpenApiExample
                    {
                        Summary = $"Error {statusCode} response with correlation ID",
                        Description = $"Example of a {statusCode} error response including correlation ID for debugging",
                        Value = new Microsoft.OpenApi.Any.OpenApiObject
                        {
                            ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(false),
                            ["error"] = new Microsoft.OpenApi.Any.OpenApiObject
                            {
                                ["type"] = new Microsoft.OpenApi.Any.OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.5.1"),
                                ["title"] = new Microsoft.OpenApi.Any.OpenApiString("Validation Error"),
                                ["detail"] = new Microsoft.OpenApi.Any.OpenApiString("Transaction ID cannot be empty"),
                                ["status"] = new Microsoft.OpenApi.Any.OpenApiInteger(int.Parse(statusCode)),
                                ["instance"] = new Microsoft.OpenApi.Any.OpenApiString("/api/v1/transactions/")
                            },
                            ["timestamp"] = new Microsoft.OpenApi.Any.OpenApiString("2024-01-15T10:30:00Z"),
                            ["correlationId"] = new Microsoft.OpenApi.Any.OpenApiString("def456-abc123-xyz789"),
                            ["version"] = new Microsoft.OpenApi.Any.OpenApiString("1.0")
                        }
                    });
                }
            }
        }
    }
}
