using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Authentication;

namespace FinancialMonitoring.Api.Tests;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ITransactionQueryService> _mockQueryService;
    private readonly string _apiKey = "a-dummy-test-api-key";

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockQueryService = new Mock<ITransactionQueryService>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "KEY_VAULT_URI", "https://dummy.keyvault.uri" },
                    { "ApiSettings:ApiKey", _apiKey },
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
        _client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.ApiKeyHeaderName, _apiKey);
    }

    [Fact]
    public async Task GetAllTransactions_ReturnsOkResult_WithListOfTransactions()
    {
        var expectedTransactions = new List<Transaction>
        {
            new Transaction("tx1", 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Account("ACC1"), new Account("ACC2")),
            new Transaction("tx2", 200, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Account("ACC3"), new Account("ACC4"))
        };

        _mockQueryService
            .Setup(service => service.GetAllTransactionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expectedTransactions);

        var response = await _client.GetAsync("/api/transactions?pageNumber=1&pageSize=2");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualTransactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
        Assert.NotNull(actualTransactions);
        Assert.Equal(expectedTransactions.Count, actualTransactions.Count);
        Assert.Contains(actualTransactions, t => t.Id == "tx1");
        Assert.Contains(actualTransactions, t => t.Id == "tx2");
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionExists_ReturnsOkResult_WithTransaction()
    {
        var transactionId = "existing-tx-id";
        var expectedTransaction = new Transaction(transactionId, 150, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Account("ACC5"), new Account("ACC6"));

        _mockQueryService
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(expectedTransaction);

        var response = await _client.GetAsync($"/api/transactions/{transactionId}");

        response.EnsureSuccessStatusCode();
        var actualTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(actualTransaction);
        Assert.Equal(expectedTransaction.Id, actualTransaction.Id);
        Assert.Equal(expectedTransaction.Amount, actualTransaction.Amount);
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
}
