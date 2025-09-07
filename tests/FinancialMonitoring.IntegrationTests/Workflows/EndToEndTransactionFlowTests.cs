using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Confluent.Kafka;
using Microsoft.Azure.Cosmos;
using FinancialMonitoring.Models;
using FinancialMonitoring.Api.Authentication;

namespace FinancialMonitoring.IntegrationTests.Workflows;

[Trait("Category", "E2E")]
public class EndToEndTransactionFlowTests : IAsyncLifetime
{
    private readonly TestConfiguration _config;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IProducer<Null, string> _producer = null!;
    private CosmosClient _cosmosClient = null!;
    private Database _database = null!;
    private Container _container = null!;

    public EndToEndTransactionFlowTests()
    {
        _config = TestConfiguration.FromEnvironment();
        _config.Validate();
    }

    public async Task InitializeAsync()
    {
        // Connect to Kafka
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config.Kafka.BootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        // Connect to CosmosDB
        var connectionString = $"AccountEndpoint={_config.CosmosDb.EndpointUri};AccountKey={_config.CosmosDb.PrimaryKey}";
        _cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
        {
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            })
        });
        _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("IntegrationTestDb");
        _container = await _database.CreateContainerIfNotExistsAsync("transactions", "/id");

        // Setup API factory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ApiSettings:ApiKey", "integration-test-key" },
                        { "CosmosDb:EndpointUri", _config.CosmosDb.EndpointUri },
                        { "CosmosDb:PrimaryKey", _config.CosmosDb.PrimaryKey },
                        { "CosmosDb:DatabaseName", "IntegrationTestDb" },
                        { "CosmosDb:ContainerName", "transactions" },
                        { "CosmosDb:PartitionKeyPath", "/id" },
                        { "Redis:ConnectionString", _config.Redis.ConnectionString },
                        { "Kafka:BootstrapServers", _config.Kafka.BootstrapServers },
                        { "AnomalyDetection:MaxAmountThreshold", "1000" },
                        { "AnomalyDetection:FrequencyThresholdPerMinute", "10" },
                        { "ApplicationInsights:ConnectionString", "" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Warning);
                });
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.ApiKeyHeaderName, "integration-test-key");
    }

    /// <summary>
    /// This test verifies the complete end-to-end transaction processing from Kafka message to API retrieval using Docker Compose environment
    /// </summary>
    [Fact]
    public async Task EndToEndTransactionFlow_ShouldProcessTransactionFromKafkaToApi()
    {
        var transaction = new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: 250.00,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            sourceAccount: new Account("ACC-FROM-001"),
            destinationAccount: new Account("ACC-TO-001"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: "Integration Test Store",
            location: new Location("NYC", "NY", "US")
        );

        await _producer.ProduceAsync("transactions", new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(transaction)
        });

        await Task.Delay(5000);

        var response = await _client.GetAsync($"/api/transactions/{transaction.Id}");

        Assert.True(response.IsSuccessStatusCode, $"Expected success status code, but got {response.StatusCode}");

        var retrievedTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(retrievedTransaction);
        Assert.Equal(transaction.Id, retrievedTransaction.Id);
        Assert.Equal(transaction.Amount, retrievedTransaction.Amount);
    }

    /// <summary>
    /// This test verifies that high-value transactions trigger anomaly detection and are flagged appropriately in the full system flow
    /// </summary>
    [Fact]
    public async Task EndToEndTransactionFlow_WithHighAmount_ShouldTriggerAnomalyDetection()
    {
        var anomalousTransaction = new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: 5000.00,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            sourceAccount: new Account("ACC-ANOMALY-001"),
            destinationAccount: new Account("ACC-ANOMALY-002"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: "High Value Store",
            location: new Location("NYC", "NY", "US")
        );

        await _producer.ProduceAsync("transactions", new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(anomalousTransaction)
        });

        await Task.Delay(5000);

        var response = await _client.GetAsync($"/api/transactions/{anomalousTransaction.Id}");
        Assert.True(response.IsSuccessStatusCode);

        var retrievedTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(retrievedTransaction);
        Assert.Equal(anomalousTransaction.Id, retrievedTransaction.Id);
        Assert.NotNull(retrievedTransaction.AnomalyFlag);
    }

    /// <summary>
    /// This test verifies that the system can handle multiple transactions processed concurrently and retrieve them via API
    /// </summary>
    [Fact]
    public async Task EndToEndTransactionFlow_MultipleTransactions_ShouldHandleVolume()
    {
        var transactions = Enumerable.Range(1, 10).Select(i => new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: 100.00 + i,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            sourceAccount: new Account($"ACC-FROM-{i:D3}"),
            destinationAccount: new Account($"ACC-TO-{i:D3}"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: $"Test Store {i}",
            location: new Location("NYC", "NY", "US")
        )).ToList();

        var tasks = transactions.Select(async transaction =>
        {
            await _producer.ProduceAsync("transactions", new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(transaction)
            });
        });

        await Task.WhenAll(tasks);
        await Task.Delay(8000);

        var response = await _client.GetAsync("/api/transactions?pageSize=20");
        Assert.True(response.IsSuccessStatusCode);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Transaction>>();
        Assert.NotNull(pagedResult);
        Assert.True(pagedResult.TotalCount >= 10, $"Expected at least 10 transactions, but got {pagedResult.TotalCount}");
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        _cosmosClient?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }
}
