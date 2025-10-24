using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;

namespace FinancialMonitoring.IntegrationTests.ApiContracts.Security;

/// <summary>
/// Integration tests for OAuthController endpoints
/// Tests OAuth2 client credentials flow and client management endpoints
/// </summary>
public class OAuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestSecretKey = "YourSuperSecretKeyForDevelopmentOnlyMustBe32CharsOrMore!";
    private const string TestIssuer = "FinancialMonitoring.Api";
    private const string TestAudience = "FinancialMonitoring.Clients";

    private const string AdminUsername = "admin";
    private const string AdminPassword = "Admin123!";

    public OAuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:SecretKey", TestSecretKey },
                    { "JwtSettings:Issuer", TestIssuer },
                    { "JwtSettings:Audience", TestAudience },
                    { "JwtSettings:AccessTokenExpiryMinutes", "15" },
                    { "JwtSettings:RefreshTokenExpiryDays", "7" },
                    { "JwtSettings:ValidateIssuer", "true" },
                    { "JwtSettings:ValidateAudience", "true" },
                    { "JwtSettings:ValidateLifetime", "true" },
                    { "JwtSettings:ClockSkewMinutes", "5" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
                services.RemoveAll<IJwtTokenService>();
                services.AddScoped<IJwtTokenService, JwtTokenService>();
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository, InMemoryUserRepository>();
                services.RemoveAll<IOAuthClientRepository>();
                services.AddSingleton<IOAuthClientRepository, InMemoryOAuthClientRepository>();
                services.RemoveAll<IOAuthClientService>();
                services.AddScoped<IOAuthClientService, OAuthClientService>();
            });
        });
    }


    [Fact]
    public async Task TokenEndpoint_WithValidClientCredentials_ShouldReturnAccessToken()
    {
        var client = _factory.CreateClient();

        var (clientId, clientSecret) = await CreateOAuthClientAsync(client);

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope", "read")
        });

        var response = await client.PostAsync("/api/v2/oauth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresIn > 0);
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidClientId_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", "invalid-client-id"),
            new KeyValuePair<string, string>("client_secret", "invalid-secret")
        });

        var response = await client.PostAsync("/api/v2/oauth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("invalid_client", error.Error);
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidClientSecret_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var (clientId, _) = await CreateOAuthClientAsync(client);

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", "wrong-secret")
        });

        var response = await client.PostAsync("/api/v2/oauth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_WithUnsupportedGrantType_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", "test"),
            new KeyValuePair<string, string>("client_secret", "test")
        });

        var response = await client.PostAsync("/api/v2/oauth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("unsupported_grant_type", error.Error);
    }

    [Fact]
    public async Task TokenEndpoint_WithSpecificScope_ShouldReturnTokenWithRequestedScope()
    {
        var client = _factory.CreateClient();

        var (clientId, clientSecret) = await CreateOAuthClientAsync(client, new[] { "read", "write", "analytics" });

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope", "read write")
        });

        var response = await client.PostAsync("/api/v2/oauth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Scope);
        Assert.Contains("read", result.Scope);
        Assert.Contains("write", result.Scope);
    }

    [Fact]
    public async Task TokenEndpoint_WithInvalidScope_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();

        var (clientId, clientSecret) = await CreateOAuthClientAsync(client, new[] { "read" });

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope", "invalid_scope")
        });

        var response = await client.PostAsync("/api/v2/oauth/token", tokenRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<OAuthErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("invalid_scope", error.Error);
    }



    [Fact]
    public async Task CreateClient_AsAdmin_ShouldCreateNewOAuthClient()
    {
        var client = _factory.CreateClient();

        var adminToken = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createRequest = new
        {
            Name = "Test OAuth Client",
            Description = "Test client for integration tests",
            AllowedScopes = new[] { "read", "write" }
        };

        var response = await client.PostAsJsonAsync("/api/v2/oauth/clients", createRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OAuthClientResponseWrapper>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ClientId);
        Assert.NotEmpty(result.Data.ClientSecret);
        Assert.NotEqual("***", result.Data.ClientSecret); // Should have actual secret on creation
        Assert.Equal("Test OAuth Client", result.Data.Name);
    }

    [Fact]
    public async Task CreateClient_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            Name = "Test Client",
            Description = "Test",
            AllowedScopes = new[] { "read" }
        };

        var response = await client.PostAsJsonAsync("/api/v2/oauth/clients", createRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClient_AsAdmin_ShouldReturnClientWithMaskedSecret()
    {
        var client = _factory.CreateClient();

        var (clientId, _) = await CreateOAuthClientAsync(client);

        var adminToken = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync($"/api/v2/oauth/clients/{clientId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OAuthClientResponseWrapper>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(clientId, result.Data.ClientId);
        Assert.Equal("***", result.Data.ClientSecret); // Secret should be masked
    }

    [Fact]
    public async Task GetClient_WithNonExistentClientId_ShouldReturnNotFound()
    {
        var client = _factory.CreateClient();

        var adminToken = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync("/api/v2/oauth/clients/non-existent-client-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllClients_AsAdmin_ShouldReturnAllClients()
    {
        var client = _factory.CreateClient();

        await CreateOAuthClientAsync(client);
        await CreateOAuthClientAsync(client);

        var adminToken = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await client.GetAsync("/api/v2/oauth/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OAuthClientsListResponseWrapper>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count >= 2);

        Assert.All(result.Data, c => Assert.Equal("***", c.ClientSecret));
    }

    [Fact]
    public async Task GetAllClients_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v2/oauth/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }



    private async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();
        return loginResult?.Data?.AccessToken ?? throw new Exception("Failed to get admin token");
    }

    private async Task<(string ClientId, string ClientSecret)> CreateOAuthClientAsync(
        HttpClient client,
        string[]? scopes = null)
    {
        var adminToken = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createRequest = new
        {
            Name = $"Test Client {Guid.NewGuid()}",
            Description = "Integration test client",
            AllowedScopes = scopes ?? new[] { "read", "write" }
        };

        var response = await client.PostAsJsonAsync("/api/v2/oauth/clients", createRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OAuthClientResponseWrapper>();
        return (result!.Data!.ClientId, result.Data.ClientSecret);
    }



    private class LoginResponseWrapper
    {
        public bool Success { get; set; }
        public LoginData? Data { get; set; }
    }

    private class LoginData
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string? Scope { get; set; }
    }

    private class OAuthErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? ErrorDescription { get; set; }
    }

    private class OAuthClientResponseWrapper
    {
        public bool Success { get; set; }
        public OAuthClientData? Data { get; set; }
    }

    private class OAuthClientData
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AllowedScopes { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    private class OAuthClientsListResponseWrapper
    {
        public bool Success { get; set; }
        public List<OAuthClientData> Data { get; set; } = new();
    }

}
