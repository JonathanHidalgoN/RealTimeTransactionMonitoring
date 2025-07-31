# API Caching and Performance Features

## Overview

The Financial Monitoring API implements enterprise-grade caching and performance optimizations to ensure fast response times and efficient resource utilization in production environments.

## Output Caching (HTTP Response Caching)

The API uses ASP.NET Core's modern OutputCache framework for HTTP-level response caching, providing significant performance improvements for frequently requested data.

### Cache Policies

#### TransactionCache
- **Duration**: 2 minutes
- **Varies By**: Query parameters (pageNumber, pageSize, startDate, endDate, minAmount, maxAmount)
- **Applied To**: Transaction listing endpoints
- **Purpose**: Cache paginated transaction results with different filtering criteria

#### TransactionByIdCache  
- **Duration**: 10 minutes
- **Varies By**: Route values (transaction ID)
- **Applied To**: Individual transaction retrieval
- **Purpose**: Cache individual transaction lookups (transactions rarely change)

#### AnomalousTransactionCache
- **Duration**: 1 minute
- **Varies By**: Query parameters (pageNumber, pageSize)
- **Applied To**: Anomalous transaction endpoints
- **Purpose**: Cache anomaly detection results with shorter TTL for fresher data

### Implementation Details

```csharp
// Program.cs configuration
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
    
    options.AddPolicy("TransactionCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(2))
               .SetVaryByQuery("pageNumber", "pageSize", "startDate", "endDate", "minAmount", "maxAmount"));
});

// Controller usage
[Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "TransactionCache")]
public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> GetAllTransactions(...)
```

### Cache Benefits

- **Response Time**: Up to 90% reduction in response time for cached requests
- **Database Load**: Significant reduction in database queries
- **Bandwidth**: Efficient use of network resources
- **Scalability**: Better handling of concurrent requests

## Response Caching Configuration

Additional response caching is configured for optimal performance:

```csharp
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB max cached response size
    options.UseCaseSensitivePaths = false;  // Improve cache hit ratio
    options.SizeLimit = 10 * 1024 * 1024;  // 10MB total cache size
});
```

## Performance Considerations

### Cache Invalidation Strategy
- **Time-based**: All caches use time-based expiration
- **Granular TTL**: Different TTL values based on data volatility
- **Query-aware**: Cache varies by query parameters to ensure data accuracy

### Production Recommendations

1. **Monitor Cache Hit Rates**: Use Application Insights to track cache effectiveness
2. **Adjust TTL Values**: Based on data freshness requirements and usage patterns
3. **Memory Monitoring**: Ensure adequate server memory for cache storage
4. **Load Testing**: Validate cache performance under realistic load conditions

### Cache Headers

The API automatically sets appropriate HTTP cache headers:
- `Cache-Control`: Specifies caching behavior
- `ETag`: Enables conditional requests
- `Last-Modified`: Supports cache validation
- `Vary`: Indicates which headers affect caching

## Memory Caching

The API also includes in-memory caching through ASP.NET Core's `IMemoryCache`:

```csharp
builder.Services.AddMemoryCache();
```

This provides application-level caching for:
- Configuration data
- Frequently accessed lookup data
- Rate limiting counters
- Health check results

## Best Practices

### When to Cache
- ✅ Read-heavy operations
- ✅ Expensive database queries
- ✅ Data that changes infrequently
- ✅ Aggregated or computed results

### When NOT to Cache
- ❌ Real-time data requirements
- ❌ User-specific sensitive data
- ❌ Frequently changing data
- ❌ Large result sets that exceed memory limits

### Cache Key Design
- Use meaningful, hierarchical cache keys
- Include version information when needed
- Consider cache key length and complexity
- Ensure keys are deterministic and consistent

## Monitoring and Debugging

### Cache Performance Metrics
- Cache hit/miss ratios
- Response time improvements
- Memory usage patterns
- Cache eviction frequency

### Debugging Tools
- Response headers indicate cache status
- Application Insights tracks cache effectiveness
- Health checks monitor cache performance
- Logging provides cache operation details

## Security Considerations

- Cached responses do not include sensitive headers
- Authentication-dependent data uses appropriate vary headers
- Cache policies respect data access permissions
- No caching of error responses containing sensitive information