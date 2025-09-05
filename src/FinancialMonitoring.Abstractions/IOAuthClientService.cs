using FinancialMonitoring.Models.OAuth;

namespace FinancialMonitoring.Abstractions;

/// <summary>
/// Service interface for OAuth client management and validation
/// </summary>
public interface IOAuthClientService
{
    /// <summary>
    /// Validates client credentials and returns the client if valid
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="clientSecret">Client secret</param>
    /// <returns>The validated client if credentials are correct and client is active, null otherwise</returns>
    Task<OAuthClient?> ValidateClientCredentialsAsync(string clientId, string clientSecret);

    /// <summary>
    /// Determines the granted scopes based on requested scopes and client permissions
    /// </summary>
    /// <param name="client">The OAuth client</param>
    /// <param name="requestedScopes">The scopes requested by the client</param>
    /// <returns>The scopes that should be granted</returns>
    IEnumerable<string> DetermineGrantedScopes(OAuthClient client, IEnumerable<string> requestedScopes);

    /// <summary>
    /// Updates the last used timestamp for a client
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateLastUsedAsync(string clientId);

    /// <summary>
    /// Creates a new OAuth client
    /// </summary>
    /// <param name="name">Client name</param>
    /// <param name="description">Client description</param>
    /// <param name="allowedScopes">Allowed scopes for the client</param>
    /// <returns>The created client with generated client_id and client_secret</returns>
    Task<OAuthClient> CreateClientAsync(string name, string description, IEnumerable<string> allowedScopes);

    /// <summary>
    /// Gets all OAuth clients (for admin purposes)
    /// </summary>
    /// <returns>List of all OAuth clients</returns>
    Task<IEnumerable<OAuthClient>> GetAllClientsAsync();
}