using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinancialMonitoring.Api.Tests;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly Mock<ITransactionRepository> _mockRepository;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockRepository = new Mock<ITransactionRepository>();
        var _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "KEY_VAULT_URI", "https://dummy.keyvault.uri" },
                    { "ApiSettings:ApiKey", "a-dummy-test-api-key" },
                    { "ApplicationInsights:ConnectionString", "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://test.in.ai.azure.com/" },
                    { "CosmosDb:EndpointUri", "https://localhost:8081" },
                    { "CosmosDb:PrimaryKey", "Cy236yDjf5/R+ob7XIw/Jw==" },
                    { "CosmosDb:DatabaseName", "TestDb" },
                    { "CosmosDb:ContainerName", "TestContainer" },
                    { "CosmosDb:PartitionKeyPath", "/id" },
                    { "MongoDb:ConnectionString", "mongodb://localhost:27017" },
                    { "MongoDb:DatabaseName", "TestFinancialMonitoring" },
                    { "MongoDb:CollectionName", "transactions" },
                    { "Kafka:BootstrapServers", "test-kafka:9092" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton<ITransactionRepository>(_mockRepository.Object);
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add(SecureApiKeyAuthenticationDefaults.ApiKeyHeaderName, "a-dummy-test-api-key");
    }

    [Fact]
    public async Task GetAllTransactions_ReturnsOkResult_WithPagedResultOfTransactions()
    {
        var expectedTransactions = new List<Transaction>
        {
            new Transaction("tx1", 100, 0, new Account("ACC1"), new Account("ACC2"),
                TransactionType.Purchase, MerchantCategory.Retail, "Store 1", new Location("NYC", "NY", "US")),
            new Transaction("tx2", 200, 0, new Account("ACC3"), new Account("ACC4"),
                TransactionType.Purchase, MerchantCategory.Grocery, "Store 2", new Location("LA", "CA", "US"))
        };

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = expectedTransactions,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 2
        };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 2))
            .ReturnsAsync(expectedPagedResult);

        var response = await _client.GetAsync($"{AppConstants.Routes.GetTransactionsPath()}?pageNumber=1&pageSize=2");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));

        var actualApiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<Transaction>>>();

        Assert.NotNull(actualApiResponse);
        Assert.True(actualApiResponse.Success);
        Assert.NotNull(actualApiResponse.Data);
        Assert.NotNull(actualApiResponse.Data.Items);
        Assert.Equal(expectedPagedResult.TotalCount, actualApiResponse.Data.TotalCount);
        Assert.Equal(expectedTransactions.Count, actualApiResponse.Data.Items.Count);
        Assert.Contains(actualApiResponse.Data.Items, t => t.Id == "tx1");
        Assert.NotNull(actualApiResponse.CorrelationId);
        Assert.Equal(AppConstants.ApiVersion, actualApiResponse.Version);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionExists_ReturnsOkResult_WithTransaction()
    {
        var transactionId = "existing-tx-id";
        var expectedTransaction = new Transaction(transactionId, 150, 0, new Account("ACC5"), new Account("ACC6"),
            TransactionType.Purchase, MerchantCategory.Restaurant, "Test Restaurant", new Location("NYC", "NY", "US"));

        _mockRepository
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(expectedTransaction);

        var response = await _client.GetAsync(AppConstants.Routes.GetTransactionByIdPath(transactionId));

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));

        var actualApiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Transaction>>();
        Assert.NotNull(actualApiResponse);
        Assert.True(actualApiResponse.Success);
        Assert.NotNull(actualApiResponse.Data);
        Assert.Equal(expectedTransaction.Id, actualApiResponse.Data.Id);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var transactionId = "non-existing-tx-id";
        _mockRepository
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        var response = await _client.GetAsync(AppConstants.Routes.GetTransactionByIdPath(transactionId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(404, errorResponse.Error.Status);
    }

    [Theory]
    [InlineData("invalid-guid-format")]
    [InlineData("123-456")]
    public async Task GetTransactionById_WithValidIdFormat_WhenNotFound_ReturnsNotFound(string validIdFormat)
    {
        _mockRepository
            .Setup(service => service.GetTransactionByIdAsync(validIdFormat))
            .ReturnsAsync((Transaction?)null);

        var response = await _client.GetAsync(AppConstants.Routes.GetTransactionByIdPath(validIdFormat));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(errorResponse);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(404, errorResponse.Error.Status);
    }

    [Fact]
    public async Task GetTransactionById_WithSpecialCharacters_HandlesCorrectly()
    {
        var testIds = new[] { "test-id-123", "abc_def", "TEST123" };

        foreach (var testId in testIds)
        {
            _mockRepository
                .Setup(service => service.GetTransactionByIdAsync(testId))
                .ReturnsAsync((Transaction?)null);

            var response = await _client.GetAsync(AppConstants.Routes.GetTransactionByIdPath(testId));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(response.Headers.Contains(AppConstants.CorrelationIdHeader));
        }
    }
}
