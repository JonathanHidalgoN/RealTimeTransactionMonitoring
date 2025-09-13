using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Models;
using FinancialMonitoring.Api.Services;

namespace FinancialMonitoring.Api.Tests.WebApi;

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
                var testConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json");
                configBuilder.AddJsonFile(testConfigPath, optional: false);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);

                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
                services.RemoveAll<IJwtTokenService>();
                services.AddScoped<IJwtTokenService, JwtTokenService>();
            });
        });
    }

    private void SetupHealthyDatabase()
    {
        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ReturnsAsync(new PagedResult<Transaction>
            {
                Items = new List<Transaction>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 1
            });
    }

    private void SetupFailedDatabase()
    {
        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 1))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));
    }

    [Fact]
    public async Task SimpleHealthCheck_ShouldReturnHealthy()
    {
        SetupHealthyDatabase();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.HealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content.Trim());
    }

    [Fact]
    public async Task DetailedHealthCheck_ShouldReturnJsonWithHealthyStatus()
    {
        SetupHealthyDatabase();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.DetailedHealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenDatabaseFails_ShouldReturnServiceUnavailable()
    {
        SetupFailedDatabase();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AppConstants.DetailedHealthCheckEndpoint);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unhealthy", content);
    }


}
