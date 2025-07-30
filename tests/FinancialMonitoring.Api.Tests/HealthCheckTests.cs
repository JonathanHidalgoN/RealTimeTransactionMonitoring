using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Tests;

/// <summary>
/// Tests for health check functionality
/// </summary>
public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ITransactionRepository> _mockRepository;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ApiSettings:ApiKey", "test-api-key-123" },
                    { "MongoDb:ConnectionString", "mongodb://localhost:27017" },
                    { "MongoDb:DatabaseName", "TestFinancialMonitoring" },
                    { "MongoDb:CollectionName", "transactions" },
                    { "ApplicationInsights:ConnectionString", "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);
            });
        });
    }

    [Fact]
    public async Task SimpleHealthCheck_ShouldReturnHealthy()
    {
        var client = _factory.CreateClient();

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ReturnsAsync(new PagedResult<Transaction>
            {
                Items = new List<Transaction>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 1
            });

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content.Trim());
    }

    [Fact]
    public async Task DetailedHealthCheck_ShouldReturnJsonWithAllChecks()
    {
        var client = _factory.CreateClient();

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ReturnsAsync(new PagedResult<Transaction>
            {
                Items = new List<Transaction>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 1
            });

        var response = await client.GetAsync(AppConstants.DetailedHealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(healthReport.TryGetProperty("status", out var status));
        Assert.Equal("Healthy", status.GetString());

        Assert.True(healthReport.TryGetProperty("timestamp", out _));
        Assert.True(healthReport.TryGetProperty("duration", out _));
        Assert.True(healthReport.TryGetProperty("checks", out var checks));

        var checksArray = checks.EnumerateArray().ToList();
        Assert.True(checksArray.Count >= 2);

        var checkNames = checksArray.Select(check =>
            check.TryGetProperty("name", out var name) ? name.GetString() : null
        ).ToList();

        Assert.Contains("api", checkNames);
        Assert.Contains("database", checkNames);
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenDatabaseFails_ShouldReturnUnhealthy()
    {
        var client = _factory.CreateClient();

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var response = await client.GetAsync(AppConstants.DetailedHealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(healthReport.TryGetProperty("status", out var status));
        Assert.Equal("Unhealthy", status.GetString());

        Assert.True(healthReport.TryGetProperty("checks", out var checks));
        var checksArray = checks.EnumerateArray().ToList();

        var databaseCheck = checksArray.FirstOrDefault(check =>
            check.TryGetProperty("name", out var name) && name.GetString() == "database");

        Assert.True(databaseCheck.ValueKind != JsonValueKind.Undefined);
        Assert.True(databaseCheck.TryGetProperty("status", out var dbStatus));
        Assert.Equal("Unhealthy", dbStatus.GetString());
    }

    [Fact]
    public async Task HealthCheck_ShouldNotRequireAuthentication()
    {
        var client = _factory.CreateClient();

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ReturnsAsync(new PagedResult<Transaction>
            {
                Items = new List<Transaction>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 1
            });

        var healthzResponse = await client.GetAsync(AppConstants.HealthCheckEndpoint);
        var healthResponse = await client.GetAsync(AppConstants.DetailedHealthCheckEndpoint);

        Assert.True(healthzResponse.IsSuccessStatusCode ||
                   healthzResponse.StatusCode == HttpStatusCode.ServiceUnavailable);
        Assert.True(healthResponse.IsSuccessStatusCode ||
                   healthResponse.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ApiHealthCheck_ShouldIncludeSystemInformation()
    {
        var client = _factory.CreateClient();

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ReturnsAsync(new PagedResult<Transaction>
            {
                Items = new List<Transaction>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 1
            });

        var response = await client.GetAsync(AppConstants.DetailedHealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(healthReport.TryGetProperty("checks", out var checks));
        var checksArray = checks.EnumerateArray().ToList();

        var apiCheck = checksArray.FirstOrDefault(check =>
            check.TryGetProperty("name", out var name) && name.GetString() == "api");

        Assert.True(apiCheck.ValueKind != JsonValueKind.Undefined);

        if (apiCheck.TryGetProperty("data", out var data))
        {
            var dataProperties = data.EnumerateObject().Select(p => p.Name).ToList();

            Assert.Contains("environment", dataProperties);
            Assert.Contains("uptime", dataProperties);
            Assert.Contains("machineName", dataProperties);
        }
    }
}
