using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.OAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialMonitoring.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/oauth")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthClientService _oauthClientService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IOAuthClientService oauthClientService,
        IJwtTokenService jwtTokenService,
        ILogger<OAuthController> logger)
    {
        _oauthClientService = oauthClientService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// OAuth2 Token Endpoint - Client Credentials Grant (RFC 6749 Section 4.4)
    /// </summary>
    /// <param name="request">Client credentials request</param>
    /// <returns>Access token response or error</returns>
    [HttpPost("token")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OAuthErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Token([FromForm] ClientCredentialsRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            if (request.GrantType != "client_credentials")
            {
                _logger.LogWarning("Unsupported grant type: {GrantType}, CorrelationId: {CorrelationId}",
                    request.GrantType, correlationId);
                return BadRequest(OAuthErrorResponse.UnsupportedGrantType(
                    $"Grant type '{request.GrantType}' is not supported. Only 'client_credentials' is supported."));
            }

            var client = await _oauthClientService.ValidateClientCredentialsAsync(request.ClientId, request.ClientSecret);
            if (client == null)
            {
                _logger.LogWarning("Client credentials validation failed for {ClientId}, CorrelationId: {CorrelationId}",
                    request.ClientId, correlationId);
                return Unauthorized(OAuthErrorResponse.InvalidClient("Invalid client credentials"));
            }
            var requestedScopes = request.GetRequestedScopes();
            var grantedScopes = _oauthClientService.DetermineGrantedScopes(client, requestedScopes);
            var grantedScopesList = grantedScopes.ToList();

            if (requestedScopes.Any() && !grantedScopesList.Any())
            {
                _logger.LogWarning("No valid scopes granted for client {ClientId}, CorrelationId: {CorrelationId}",
                    request.ClientId, correlationId);
                return BadRequest(OAuthErrorResponse.InvalidScope("None of the requested scopes are valid for this client"));
            }

            var accessToken = _jwtTokenService.GenerateClientAccessToken(client, grantedScopesList);
            var expiresIn = _jwtTokenService.GetAccessTokenExpirationSeconds();

            await _oauthClientService.UpdateLastUsedAsync(client.ClientId);

            var tokenResponse = TokenResponse.ForClientCredentials(accessToken, expiresIn, grantedScopesList);

            _logger.LogInformation("Client credentials grant successful for client {ClientId} with {ScopeCount} scopes, CorrelationId: {CorrelationId}",
                client.ClientId, grantedScopesList.Count, correlationId);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing client credentials grant for client {ClientId}, CorrelationId: {CorrelationId}",
                request.ClientId, correlationId);
            return StatusCode(500, OAuthErrorResponse.InvalidRequest("An internal error occurred processing the request"));
        }
    }

    /// <summary>
    /// Creates a new OAuth client (Admin only)
    /// </summary>
    /// <param name="request">Client creation request</param>
    /// <returns>Created client information</returns>
    [HttpPost("clients")]
    [Authorize(Policy = AppConstants.AdminRole, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + SecureApiKeyAuthenticationDefaults.SchemeName)]
    [ProducesResponseType(typeof(ApiResponse<OAuthClientResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OAuthClientResponse>>> CreateClient([FromBody] CreateClientRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var client = await _oauthClientService.CreateClientAsync(
                request.Name,
                request.Description ?? "",
                request.AllowedScopes ?? new[] { "read" });

            var response = new OAuthClientResponse
            {
                Id = client.Id,
                ClientId = client.ClientId,
                ClientSecret = client.ClientSecret,
                Name = client.Name,
                Description = client.Description,
                AllowedScopes = client.GetAllowedScopes().ToList(),
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt
            };

            _logger.LogInformation("OAuth client created: {ClientId} by admin {AdminId}, CorrelationId: {CorrelationId}",
                client.ClientId, adminUserId, correlationId);

            return CreatedAtAction(nameof(GetClient), new { clientId = client.ClientId },
                ApiResponse<OAuthClientResponse>.SuccessResponse(response, correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OAuth client by admin {AdminId}, CorrelationId: {CorrelationId}",
                adminUserId, correlationId);
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                FinancialMonitoring.Models.ProblemDetails.InternalServerError("An error occurred creating the OAuth client"), correlationId));
        }
    }

    /// <summary>
    /// Gets OAuth client information (Admin only)
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Client information (without secret)</returns>
    [HttpGet("clients/{clientId}")]
    [Authorize(Policy = AppConstants.AdminRole, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + SecureApiKeyAuthenticationDefaults.SchemeName)]
    [ProducesResponseType(typeof(ApiResponse<OAuthClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OAuthClientResponse>>> GetClient(string clientId)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var clients = await _oauthClientService.GetAllClientsAsync();
            var client = clients.FirstOrDefault(c => c.ClientId == clientId);

            if (client == null)
            {
                return NotFound(ApiErrorResponse.FromProblemDetails(
                    FinancialMonitoring.Models.ProblemDetails.NotFound($"OAuth client '{clientId}' not found"), correlationId));
            }

            var response = new OAuthClientResponse
            {
                Id = client.Id,
                ClientId = client.ClientId,
                ClientSecret = "***", // Never return the actual secret
                Name = client.Name,
                Description = client.Description,
                AllowedScopes = client.GetAllowedScopes().ToList(),
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                LastUsedAt = client.LastUsedAt
            };

            return Ok(ApiResponse<OAuthClientResponse>.SuccessResponse(response, correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OAuth client {ClientId}, CorrelationId: {CorrelationId}",
                clientId, correlationId);
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                FinancialMonitoring.Models.ProblemDetails.InternalServerError("An error occurred retrieving the OAuth client"), correlationId));
        }
    }

    /// <summary>
    /// Lists all OAuth clients (Admin only)
    /// </summary>
    /// <returns>List of OAuth clients (without secrets)</returns>
    [HttpGet("clients")]
    [Authorize(Policy = AppConstants.AdminRole, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + SecureApiKeyAuthenticationDefaults.SchemeName)]
    [ProducesResponseType(typeof(ApiResponse<List<OAuthClientResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<OAuthClientResponse>>>> GetAllClients()
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var clients = await _oauthClientService.GetAllClientsAsync();
            var responses = clients.Select(client => new OAuthClientResponse
            {
                Id = client.Id,
                ClientId = client.ClientId,
                ClientSecret = "***",
                Name = client.Name,
                Description = client.Description,
                AllowedScopes = client.GetAllowedScopes().ToList(),
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                LastUsedAt = client.LastUsedAt
            }).ToList();

            return Ok(ApiResponse<List<OAuthClientResponse>>.SuccessResponse(responses, correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OAuth clients, CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, ApiErrorResponse.FromProblemDetails(
                FinancialMonitoring.Models.ProblemDetails.InternalServerError("An error occurred retrieving OAuth clients"), correlationId));
        }
    }
}
