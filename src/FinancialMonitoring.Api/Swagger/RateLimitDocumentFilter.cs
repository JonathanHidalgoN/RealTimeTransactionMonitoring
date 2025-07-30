using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinancialMonitoring.Api.Swagger;

/// <summary>
/// Swagger document filter to document rate limiting information
/// </summary>
public class RateLimitDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add rate limiting information to the API documentation
        if (swaggerDoc.Info.Extensions == null)
        {
            swaggerDoc.Info.Extensions = new Dictionary<string, Microsoft.OpenApi.Interfaces.IOpenApiExtension>();
        }

        // Add rate limiting information as extension
        swaggerDoc.Info.Extensions.Add("x-rate-limits", new Microsoft.OpenApi.Any.OpenApiObject
        {
            ["general"] = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["limit"] = new Microsoft.OpenApi.Any.OpenApiInteger(1000),
                ["period"] = new Microsoft.OpenApi.Any.OpenApiString("1 minute"),
                ["description"] = new Microsoft.OpenApi.Any.OpenApiString("General API rate limit")
            },
            ["transactions"] = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["limit"] = new Microsoft.OpenApi.Any.OpenApiInteger(100),
                ["period"] = new Microsoft.OpenApi.Any.OpenApiString("1 minute"),
                ["description"] = new Microsoft.OpenApi.Any.OpenApiString("Transaction endpoints rate limit")
            }
        });

        // Add 429 response to all operations
        foreach (var pathItem in swaggerDoc.Paths.Values)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Responses.TryAdd("429", new OpenApiResponse
                {
                    Description = "Too Many Requests - Rate limit exceeded",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.Schema,
                                    Id = "ApiErrorResponse"
                                }
                            }
                        }
                    },
                    Headers = new Dictionary<string, OpenApiHeader>
                    {
                        ["X-RateLimit-Limit"] = new OpenApiHeader
                        {
                            Description = "The rate limit ceiling for this endpoint",
                            Schema = new OpenApiSchema { Type = "integer" }
                        },
                        ["X-RateLimit-Remaining"] = new OpenApiHeader
                        {
                            Description = "The number of requests left for the time window",
                            Schema = new OpenApiSchema { Type = "integer" }
                        },
                        ["X-RateLimit-Reset"] = new OpenApiHeader
                        {
                            Description = "The remaining window before the rate limit resets (in UTC epoch seconds)",
                            Schema = new OpenApiSchema { Type = "integer" }
                        },
                        ["Retry-After"] = new OpenApiHeader
                        {
                            Description = "The number of seconds to wait before making another request",
                            Schema = new OpenApiSchema { Type = "integer" }
                        }
                    }
                });
            }
        }
    }
}
