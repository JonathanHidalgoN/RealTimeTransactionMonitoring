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

namespace FinancialMonitoring.Api.Tests;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly Mock<ITransactionQueryService> _mockQueryService;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockQueryService = new Mock<ITransactionQueryService>();
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
                    { "Kafka:BootstrapServers", "test-kafka:9092" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionQueryService>();
                services.AddSingleton<ITransactionQueryService>(_mockQueryService.Object);
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.ApiKeyHeaderName, "a-dummy-test-api-key");
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

        _mockQueryService
            .Setup(service => service.GetAllTransactionsAsync(1, 2))
            .ReturnsAsync(expectedPagedResult);

        var response = await _client.GetAsync("/api/transactions?pageNumber=1&pageSize=2");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualResult = await response.Content.ReadFromJsonAsync<PagedResult<Transaction>>();

        Assert.NotNull(actualResult);
        Assert.NotNull(actualResult.Items);
        Assert.Equal(expectedPagedResult.TotalCount, actualResult.TotalCount);
        Assert.Equal(expectedTransactions.Count, actualResult.Items.Count);
        Assert.Contains(actualResult.Items, t => t.Id == "tx1");
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionExists_ReturnsOkResult_WithTransaction()
    {
        var transactionId = "existing-tx-id";
        var expectedTransaction = new Transaction(transactionId, 150, 0, new Account("ACC5"), new Account("ACC6"),
            TransactionType.Purchase, MerchantCategory.Restaurant, "Test Restaurant", new Location("NYC", "NY", "US"));

        _mockQueryService
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(expectedTransaction);

        var response = await _client.GetAsync($"/api/transactions/{transactionId}");

        response.EnsureSuccessStatusCode();
        var actualTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(actualTransaction);
        Assert.Equal(expectedTransaction.Id, actualTransaction.Id);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var transactionId = "non-existing-tx-id";
        _mockQueryService
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        var response = await _client.GetAsync($"/api/transactions/{transactionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetTransactionById_WithInvalidId_ReturnsBadRequest(string invalidId)
    {
        var controller = new Controllers.TransactionsController(_mockQueryService.Object, Mock.Of<ILogger<Controllers.TransactionsController>>());

        var result = await controller.GetTransactionById(invalidId);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
