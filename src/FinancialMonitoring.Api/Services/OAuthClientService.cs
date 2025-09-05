using System.Security.Cryptography;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Models.OAuth;
using FinancialMonitoring.Abstractions.Persistence;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// Service for OAuth client management and validation
/// </summary>
public class OAuthClientService : IOAuthClientService
{
    private readonly IOAuthClientRepository _oauthClientRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILogger<OAuthClientService> _logger;

    public OAuthClientService(
        IOAuthClientRepository oauthClientRepository,
        IPasswordHashingService passwordHashingService,
        ILogger<OAuthClientService> logger)
    {
        _oauthClientRepository = oauthClientRepository;
        _passwordHashingService = passwordHashingService;
        _logger = logger;
    }

    public async Task<OAuthClient?> ValidateClientCredentialsAsync(string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Client credentials validation failed: missing clientId or clientSecret");
            return null;
        }

        try
        {
            var client = await _oauthClientRepository.GetByClientIdAsync(clientId);
            if (client == null)
            {
                _logger.LogWarning("Client credentials validation failed: client {ClientId} not found", clientId);
                return null;
            }

            if (!client.IsActive)
            {
                _logger.LogWarning("Client credentials validation failed: client {ClientId} is inactive", clientId);
                return null;
            }

            if (!_passwordHashingService.VerifyPassword(clientSecret, client.ClientSecret, "oauth_clients"))
            {
                _logger.LogWarning("Client credentials validation failed: invalid secret for client {ClientId}", clientId);
                return null;
            }

            _logger.LogInformation("Client credentials validated successfully for client {ClientId}", clientId);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating client credentials for client {ClientId}", clientId);
            return null;
        }
    }

    public IEnumerable<string> DetermineGrantedScopes(OAuthClient client, IEnumerable<string> requestedScopes)
    {
        var allowedScopes = client.GetAllowedScopes().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requested = requestedScopes.ToList();

        if (!requested.Any())
        {
            _logger.LogDebug("No scopes requested for client {ClientId}, granting all allowed scopes", client.ClientId);
            return allowedScopes;
        }

        var grantedScopes = requested.Where(scope => allowedScopes.Contains(scope)).ToList();

        _logger.LogDebug("Client {ClientId} requested {RequestedCount} scopes, granted {GrantedCount} scopes",
            client.ClientId, requested.Count, grantedScopes.Count);

        return grantedScopes;
    }

    public async Task UpdateLastUsedAsync(string clientId)
    {
        try
        {
            await _oauthClientRepository.UpdateLastUsedAsync(clientId);
            _logger.LogDebug("Updated last used timestamp for client {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last used timestamp for client {ClientId}", clientId);
        }
    }

    public async Task<OAuthClient> CreateClientAsync(string name, string description, IEnumerable<string> allowedScopes)
    {
        var clientId = GenerateClientId();
        var clientSecret = GenerateClientSecret();

        var hashedSecret = _passwordHashingService.HashPassword(clientSecret, "oauth_clients");

        var client = new OAuthClient
        {
            ClientId = clientId,
            ClientSecret = hashedSecret,
            Name = name,
            Description = description,
            AllowedScopes = string.Join(",", allowedScopes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdClient = await _oauthClientRepository.CreateAsync(client);

        createdClient.ClientSecret = clientSecret;

        _logger.LogInformation("Created new OAuth client {ClientId} with name '{Name}'", clientId, name);

        return createdClient;
    }

    public async Task<IEnumerable<OAuthClient>> GetAllClientsAsync()
    {
        return await _oauthClientRepository.GetAllAsync();
    }

    private static string GenerateClientId()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
                     .Replace("+", "-")
                     .Replace("/", "_")
                     .Replace("=", "");
    }

    private static string GenerateClientSecret()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
