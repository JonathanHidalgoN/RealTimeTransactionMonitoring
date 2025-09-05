using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models.OAuth;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancialMonitoring.Api.Tests.V2;

public class OAuthClientServiceV2Tests
{
    private readonly OAuthClientService _service;
    private readonly Mock<IOAuthClientRepository> _mockRepository;
    private readonly Mock<IPasswordHashingService> _mockPasswordService;
    private readonly Mock<ILogger<OAuthClientService>> _mockLogger;

    public OAuthClientServiceV2Tests()
    {
        _mockRepository = new Mock<IOAuthClientRepository>();
        _mockPasswordService = new Mock<IPasswordHashingService>();
        _mockLogger = new Mock<ILogger<OAuthClientService>>();
        
        _service = new OAuthClientService(
            _mockRepository.Object,
            _mockPasswordService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_WithValidCredentials_ReturnsClient()
    {
        var clientId = "test-client";
        var clientSecret = "test-secret";
        var hashedSecret = "hashed-secret";
        
        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = clientId,
            ClientSecret = hashedSecret,
            Name = "Test Client",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByClientIdAsync(clientId))
            .ReturnsAsync(testClient);

        _mockPasswordService
            .Setup(p => p.VerifyPassword(clientSecret, hashedSecret, "oauth_clients"))
            .Returns(true);

        var result = await _service.ValidateClientCredentialsAsync(clientId, clientSecret);

        Assert.NotNull(result);
        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(hashedSecret, result.ClientSecret);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_WithInvalidClientId_ReturnsNull()
    {
        var clientId = "invalid-client";
        var clientSecret = "test-secret";

        _mockRepository
            .Setup(r => r.GetByClientIdAsync(clientId))
            .ReturnsAsync((OAuthClient?)null);

        var result = await _service.ValidateClientCredentialsAsync(clientId, clientSecret);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_WithInactiveClient_ReturnsNull()
    {
        var clientId = "test-client";
        var clientSecret = "test-secret";
        
        var inactiveClient = new OAuthClient
        {
            Id = 1,
            ClientId = clientId,
            ClientSecret = "hashed-secret",
            Name = "Test Client",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByClientIdAsync(clientId))
            .ReturnsAsync(inactiveClient);

        var result = await _service.ValidateClientCredentialsAsync(clientId, clientSecret);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_WithInvalidSecret_ReturnsNull()
    {
        var clientId = "test-client";
        var clientSecret = "invalid-secret";
        var hashedSecret = "hashed-secret";
        
        var testClient = new OAuthClient
        {
            Id = 1,
            ClientId = clientId,
            ClientSecret = hashedSecret,
            Name = "Test Client",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByClientIdAsync(clientId))
            .ReturnsAsync(testClient);

        _mockPasswordService
            .Setup(p => p.VerifyPassword(clientSecret, hashedSecret, "oauth_clients"))
            .Returns(false);

        var result = await _service.ValidateClientCredentialsAsync(clientId, clientSecret);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null, "secret")]
    [InlineData("client", null)]
    [InlineData("", "secret")]
    [InlineData("client", "")]
    public async Task ValidateClientCredentialsAsync_WithNullOrEmptyCredentials_ReturnsNull(string? clientId, string? clientSecret)
    {
        var result = await _service.ValidateClientCredentialsAsync(clientId!, clientSecret!);

        Assert.Null(result);
    }

    [Fact]
    public void DetermineGrantedScopes_WithNoRequestedScopes_ReturnsAllAllowedScopes()
    {
        var client = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            AllowedScopes = "read,write,analytics"
        };

        var requestedScopes = Enumerable.Empty<string>();

        var result = _service.DetermineGrantedScopes(client, requestedScopes);

        var grantedScopes = result.ToList();
        Assert.Equal(3, grantedScopes.Count);
        Assert.Contains("read", grantedScopes);
        Assert.Contains("write", grantedScopes);
        Assert.Contains("analytics", grantedScopes);
    }

    [Fact]
    public void DetermineGrantedScopes_WithValidRequestedScopes_ReturnsIntersection()
    {
        var client = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            AllowedScopes = "read,write,analytics"
        };

        var requestedScopes = new[] { "read", "admin" }; // admin is not allowed

        var result = _service.DetermineGrantedScopes(client, requestedScopes);

        var grantedScopes = result.ToList();
        Assert.Single(grantedScopes);
        Assert.Contains("read", grantedScopes);
        Assert.DoesNotContain("admin", grantedScopes);
    }

    [Fact]
    public void DetermineGrantedScopes_WithNoValidRequestedScopes_ReturnsEmpty()
    {
        var client = new OAuthClient
        {
            Id = 1,
            ClientId = "test-client",
            AllowedScopes = "read,write"
        };

        var requestedScopes = new[] { "admin", "delete" }; // None are allowed

        var result = _service.DetermineGrantedScopes(client, requestedScopes);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateClientAsync_WithValidData_ReturnsCreatedClient()
    {
        var name = "Test Client";
        var description = "Test Description";
        var allowedScopes = new[] { "read", "write" };
        var hashedSecret = "hashed-secret";

        _mockPasswordService
            .Setup(p => p.HashPassword(It.IsAny<string>(), "oauth_clients"))
            .Returns(hashedSecret);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<OAuthClient>()))
            .ReturnsAsync((OAuthClient client) =>
            {
                client.Id = 1;
                return client;
            });

        var result = await _service.CreateClientAsync(name, description, allowedScopes);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal("read,write", result.AllowedScopes);
        Assert.True(result.IsActive);
        Assert.NotEmpty(result.ClientId);
        Assert.NotEmpty(result.ClientSecret); // This should be the plain-text secret for the response
    }

    [Fact]
    public async Task UpdateLastUsedAsync_CallsRepository()
    {
        var clientId = "test-client";

        await _service.UpdateLastUsedAsync(clientId);

        _mockRepository.Verify(r => r.UpdateLastUsedAsync(clientId), Times.Once);
    }

    [Fact]
    public async Task GetAllClientsAsync_CallsRepository()
    {
        var clients = new List<OAuthClient>
        {
            new OAuthClient { Id = 1, ClientId = "client1", Name = "Client 1" },
            new OAuthClient { Id = 2, ClientId = "client2", Name = "Client 2" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(clients);

        var result = await _service.GetAllClientsAsync();

        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }
}