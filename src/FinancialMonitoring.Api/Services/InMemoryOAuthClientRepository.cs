using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models.OAuth;
using System.Collections.Concurrent;

namespace FinancialMonitoring.Api.Services;

/// <summary>
/// In-memory implementation of OAuth client repository for development/testing
/// </summary>
public class InMemoryOAuthClientRepository : IOAuthClientRepository
{
    //Two dirs to lookup in O(1)
    private readonly ConcurrentDictionary<int, OAuthClient> _clients = new();
    private readonly ConcurrentDictionary<string, int> _clientIdIndex = new();
    private int _nextId = 1;

    public InMemoryOAuthClientRepository()
    {
        SeedDefaultClients();
    }

    public Task<OAuthClient?> GetByClientIdAsync(string clientId)
    {
        if (_clientIdIndex.TryGetValue(clientId, out var id) && _clients.TryGetValue(id, out var client))
        {
            return Task.FromResult<OAuthClient?>(client);
        }
        return Task.FromResult<OAuthClient?>(null);
    }

    public Task<OAuthClient> CreateAsync(OAuthClient client)
    {
        var id = Interlocked.Increment(ref _nextId);
        client.Id = id;

        _clients[id] = client;
        _clientIdIndex[client.ClientId] = id;

        return Task.FromResult(client);
    }

    public Task<OAuthClient> UpdateAsync(OAuthClient client)
    {
        if (!_clients.ContainsKey(client.Id))
        {
            throw new InvalidOperationException($"OAuth client with ID {client.Id} not found");
        }

        client.UpdatedAt = DateTime.UtcNow;
        _clients[client.Id] = client;

        return Task.FromResult(client);
    }

    public Task UpdateLastUsedAsync(string clientId)
    {
        if (_clientIdIndex.TryGetValue(clientId, out var id) && _clients.TryGetValue(id, out var client))
        {
            client.LastUsedAt = DateTime.UtcNow;
            _clients[id] = client;
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<OAuthClient>> GetAllAsync()
    {
        return Task.FromResult(_clients.Values.AsEnumerable());
    }

    private void SeedDefaultClients()
    {
        var defaultClient = new OAuthClient
        {
            Id = 1,
            ClientId = "default-client",
            ClientSecret = "hashed-secret-placeholder",
            Name = "Default Development Client",
            Description = "Default OAuth client for development and testing",
            AllowedScopes = "read,write,analytics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _clients[1] = defaultClient;
        _clientIdIndex["default-client"] = 1;
        _nextId = 2;
    }
}
