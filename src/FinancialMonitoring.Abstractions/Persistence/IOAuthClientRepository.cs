using FinancialMonitoring.Models.OAuth;

namespace FinancialMonitoring.Abstractions.Persistence;

/// <summary>
/// Repository interface for OAuth client operations
/// </summary>
public interface IOAuthClientRepository
{
    /// <summary>
    /// Gets an OAuth client by client ID
    /// </summary>
    /// <param name="clientId">The client identifier</param>
    /// <returns>The OAuth client if found, null otherwise</returns>
    Task<OAuthClient?> GetByClientIdAsync(string clientId);

    /// <summary>
    /// Creates a new OAuth client
    /// </summary>
    /// <param name="client">The client to create</param>
    /// <returns>The created client with assigned ID</returns>
    Task<OAuthClient> CreateAsync(OAuthClient client);

    /// <summary>
    /// Updates an existing OAuth client
    /// </summary>
    /// <param name="client">The client to update</param>
    /// <returns>The updated client</returns>
    Task<OAuthClient> UpdateAsync(OAuthClient client);

    /// <summary>
    /// Updates the last used timestamp for a client
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateLastUsedAsync(string clientId);

    /// <summary>
    /// Gets all OAuth clients (for admin purposes)
    /// </summary>
    /// <returns>List of all OAuth clients</returns>
    Task<IEnumerable<OAuthClient>> GetAllAsync();
}
