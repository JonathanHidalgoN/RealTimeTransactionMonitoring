using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FinancialMonitoring.Api.Authentication;

public static class SecureApiKeyAuthenticationDefaults
{
    public const string SchemeName = "SecureApiKey";
    public const string ApiKeyHeaderName = "X-Api-Key";
}

//https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0
//https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme?view=aspnetcore-9.0&source=recommendations
public class SecureApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<SecureApiKeyAuthenticationHandler> _logger;

    public SecureApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiSettings> apiSettings) : base(options, logger, encoder)
    {
        _apiSettings = apiSettings.Value;
        _logger = logger.CreateLogger<SecureApiKeyAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var correlationId = Context.TraceIdentifier;
        
        if (!Request.Headers.TryGetValue(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            _logger.LogWarning("API key header missing for request {CorrelationId}", correlationId);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedApiKey))
        {
            _logger.LogWarning("Empty API key provided for request {CorrelationId}", correlationId);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
        }

        // Secure constant-time comparison to prevent timing attacks
        if (!SecureStringCompare(providedApiKey, _apiSettings.ApiKey ?? string.Empty))
        {
            _logger.LogWarning("Invalid API key provided for request {CorrelationId}", correlationId);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiService"),
            new Claim("CorrelationId", correlationId),
            new Claim("AuthenticatedAt", DateTimeOffset.UtcNow.ToString("O"))
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("Successful API key authentication for request {CorrelationId}", correlationId);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Performs constant-time string comparison to prevent timing attacks
    /// </summary>
    private static bool SecureStringCompare(string provided, string expected)
    {
        if (provided == null || expected == null)
            return false;

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.Append("WWW-Authenticate", $"{Scheme.Name} realm=\"FinancialMonitoring API\"");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}