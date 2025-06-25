using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialMonitoring.Api.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string SchemeName = "ApiKey";
    public const string ApiKeyHeaderName = "X-Api-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApiSettings _apiSettings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiSettings> apiSettings) : base(options, logger, encoder)
    {
        _apiSettings = apiSettings.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedApiKey) || !providedApiKey.Equals(_apiSettings.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiService") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
