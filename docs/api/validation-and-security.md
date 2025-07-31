# API Validation and Security Features

## Overview

The Financial Monitoring API implements comprehensive validation and security measures to ensure data integrity, prevent attacks, and maintain API reliability in production environments.

## Input Validation with FluentValidation

### Validation Framework
The API uses FluentValidation for robust, declarative input validation that provides:
- Clear, readable validation rules
- Comprehensive error messages
- Async validation support
- Custom validation logic
- Integration with ASP.NET Core model binding

### Validation Models

#### TransactionQueryRequest
Validates transaction query parameters to ensure safe and efficient database operations:

```csharp
public class TransactionQueryRequestValidator : AbstractValidator<TransactionQueryRequest>
{
    public TransactionQueryRequestValidator()
    {
        // Pagination validation
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");
            
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");
            
        // Date range validation
        RuleFor(x => x)
            .Must(HaveValidDateRange)
            .WithMessage("Start date must be earlier than end date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
            
        // Amount range validation
        RuleFor(x => x)
            .Must(HaveValidAmountRange)
            .WithMessage("Minimum amount must be less than maximum amount")
            .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue);
    }
}
```

#### Validation Rules

**Pagination Controls**:
- Page number: Must be > 0
- Page size: Between 1-100 (prevents excessive resource usage)

**Date Filtering**:
- Start date must be before end date
- Date format validation
- Future date restrictions where applicable

**Amount Filtering**:
- Minimum amount must be less than maximum amount
- Decimal precision validation
- Negative amount handling

### Automatic Validation Integration

The API automatically validates all incoming requests using FluentValidation:

```csharp
// Program.cs configuration
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

**Benefits**:
- Automatic model validation before controller actions
- Consistent error response format
- Early request rejection for invalid data
- Reduced boilerplate validation code

### Validation Error Response Format

Invalid requests return standardized error responses:

```json
{
    "success": false,
    "message": "Validation failed",
    "errors": [
        {
            "propertyName": "PageSize",
            "errorMessage": "Page size must be between 1 and 100",
            "attemptedValue": 150
        }
    ],
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## Security Headers Implementation

### Production Security Headers
The SecurityHeadersMiddleware adds essential security headers to all API responses:

#### Content Security Policy (CSP)
```
Content-Security-Policy: default-src 'self'
```
- Prevents XSS attacks by controlling resource loading
- Restricts script execution to same origin
- Blocks inline scripts and eval() usage

#### Transport Security
```
Strict-Transport-Security: max-age=31536000; includeSubDomains
```
- Enforces HTTPS connections for 1 year
- Includes all subdomains
- Prevents protocol downgrade attacks

#### Content Type Protection
```
X-Content-Type-Options: nosniff
```
- Prevents MIME type sniffing attacks
- Forces browsers to respect declared content types

#### Frame Protection
```
X-Frame-Options: DENY
```
- Prevents clickjacking attacks
- Blocks page embedding in frames/iframes

#### XSS Protection
```
X-XSS-Protection: 1; mode=block
```
- Enables browser XSS filtering
- Blocks pages when XSS is detected

#### Referrer Policy
```
Referrer-Policy: strict-origin-when-cross-origin
```
- Controls referrer information sharing
- Protects sensitive URL parameters

### Security Headers Testing

Comprehensive tests validate security header implementation:

```csharp
[Fact]
public async Task SecurityHeaders_ShouldBeAppliedToAllResponses()
{
    var response = await _client.GetAsync("/health");
    
    Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
    Assert.Contains("DENY", response.Headers.GetValues("X-Frame-Options"));
    Assert.Contains("max-age=31536000", response.Headers.GetValues("Strict-Transport-Security").First());
    // Additional assertions...
}
```

## API Key Authentication

### Authentication Scheme
The API implements secure API key authentication:

```csharp
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, SecureApiKeyAuthenticationHandler>(
        SecureApiKeyAuthenticationDefaults.SchemeName,
        options => { });
```

### Security Features
- Secure key comparison using time-constant algorithms
- Key validation against configured values
- Automatic request rejection for invalid keys
- Integration with ASP.NET Core authorization

### Usage
Clients must include the API key in request headers:
```
X-Api-Key: your-api-key-here
```

## Rate Limiting Protection

### Rate Limiting Configuration
```csharp
options.GeneralRules = new List<RateLimitRule>
{
    new RateLimitRule
    {
        Endpoint = "*",
        Period = "1m",
        Limit = 1000
    },
    new RateLimitRule
    {
        Endpoint = "*/transactions", 
        Period = "1m",
        Limit = 100
    }
};
```

### Protection Features
- IP-based rate limiting
- Endpoint-specific limits
- Configurable time windows
- Automatic request throttling
- 429 (Too Many Requests) responses

## Error Handling Security

### Global Exception Handling
The GlobalExceptionHandlingMiddleware provides secure error handling:

**Security Features**:
- Prevents sensitive information disclosure
- Logs detailed errors internally
- Returns generic error messages to clients
- Maintains consistent error response format
- Includes correlation IDs for troubleshooting

**Error Response Sanitization**:
```csharp
// Internal logging (detailed)
_logger.LogError(ex, "Database operation failed for request {CorrelationId}", correlationId);

// Client response (sanitized) 
return new ApiResponse<object>
{
    Success = false,
    Message = "An error occurred while processing the request",
    CorrelationId = correlationId
};
```

## CORS Security

### Cross-Origin Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
```

### Security Controls
- Explicit origin allowlist (no wildcards in production)
- Configurable allowed origins via settings
- Method and header restrictions
- Credential handling controls

## Security Testing Strategy

### Automated Security Tests
- Validation rule verification
- Security header presence checks
- Authentication mechanism testing
- Rate limiting functionality
- Error handling security

### Security Scanning
- Dependency vulnerability scanning
- OWASP security testing
- Penetration testing support
- Security header validation tools

## Best Practices Implementation

### Input Validation
✅ Validate all input parameters
✅ Use parameterized queries (via Entity Framework)
✅ Implement range and format validation
✅ Sanitize output data
✅ Validate business logic constraints

### Authentication & Authorization
✅ Secure API key storage and comparison
✅ Time-constant authentication algorithms
✅ Principle of least privilege
✅ Session management best practices

### Security Headers
✅ Comprehensive security header implementation
✅ Regular security header updates
✅ Browser compatibility considerations
✅ Security policy testing and validation

### Error Handling
✅ Secure error message handling
✅ Detailed internal logging
✅ Generic client error responses
✅ Correlation ID tracking
✅ No sensitive data exposure

## Compliance and Standards

The API security implementation follows industry standards:
- **OWASP API Security Top 10** compliance
- **NIST Cybersecurity Framework** alignment
- **ISO 27001** security controls
- **SOC 2** security requirements
- **PCI DSS** data protection standards (where applicable)