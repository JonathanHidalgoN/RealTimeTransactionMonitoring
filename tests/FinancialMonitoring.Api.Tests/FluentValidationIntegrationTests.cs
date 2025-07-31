using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Authentication;

namespace FinancialMonitoring.Api.Tests;

/// <summary>
/// Integration tests to verify FluentValidation is working in the API pipeline
/// </summary>
public class FluentValidationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ITransactionRepository> _mockRepository;

    public FluentValidationIntegrationTests(WebApplicationFactory<Program> factory)
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

    [Theory]
    [InlineData(0)] // Invalid: page number must be >= 1
    [InlineData(-1)] // Invalid: negative page number
    [InlineData(10001)] // Invalid: exceeds maximum of 10000
    public async Task GetAllTransactions_InvalidPageNumber_ShouldReturnBadRequest(int pageNumber)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var response = await client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber={pageNumber}&pageSize=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", content.ToLower());
    }

    [Theory]
    [InlineData(0)] // Invalid: page size must be >= 1
    [InlineData(-1)] // Invalid: negative page size
    [InlineData(101)] // Invalid: exceeds maximum of 100
    public async Task GetAllTransactions_InvalidPageSize_ShouldReturnBadRequest(int pageSize)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var response = await client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber=1&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", content.ToLower());
    }

    [Fact]
    public async Task GetAllTransactions_ValidParameters_ShouldSucceed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = new List<Transaction>(),
            TotalCount = 0,
            PageNumber = 5,
            PageSize = 50
        };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(5, 50))
            .ReturnsAsync(expectedPagedResult);

        var response = await client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber=5&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllTransactions_FutureStartDate_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber=1&pageSize=20&startDate={futureDate}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("future", content.ToLower());
    }

    [Fact]
    public async Task GetAllTransactions_NegativeMinAmount_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "test-api-key-123");

        var response = await client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber=1&pageSize=20&minAmount=-100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("negative", content.ToLower());
    }
}
