using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Testcontainers.Kafka;
using Testcontainers.CosmosDb;
using Testcontainers.Redis;
using Confluent.Kafka;
using Microsoft.Azure.Cosmos;
using FinancialMonitoring.Models;
using FinancialMonitoring.Api.Authentication;

namespace FinancialMonitoring.IntegrationTests;

public class EndToEndTransactionFlowTests : IAsyncLifetime
{
    private readonly bool _useTestContainers;
    private readonly KafkaContainer? _kafkaContainer;
    private readonly CosmosDbContainer? _cosmosContainer;
    private readonly RedisContainer? _redisContainer;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IProducer<Null, string> _producer = null!;
    private CosmosClient _cosmosClient = null!;
    private Database _database = null!;
    private Container _container = null!;

    public EndToEndTransactionFlowTests()
    {
        var config = TestConfiguration.FromEnvironment();
        // Check if we're running in Docker environment (integration tests container)
        _useTestContainers = config.Environment.UseTestContainers;

        if (_useTestContainers)
        {
            //Use testcontainer to setup the services if we are not in docker
            _kafkaContainer = new KafkaBuilder()
                .WithImage("confluentinc/cp-kafka:7.6.1")
                .Build();

            _cosmosContainer = new CosmosDbBuilder()
                .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
                .Build();

            _redisContainer = new RedisBuilder()
                .WithImage("redis:7-alpine")
                .Build();
        }
    }

    public async Task InitializeAsync()
    {
        string kafkaBootstrapServers;
        string cosmosEndpoint;
        string cosmosKey;
        string redisConnectionString;

        if (_useTestContainers)
        {
            //Use TestContainers
            await _kafkaContainer!.StartAsync();
            await _cosmosContainer!.StartAsync();
            await _redisContainer!.StartAsync();

            kafkaBootstrapServers = _kafkaContainer.GetBootstrapAddress();
            cosmosEndpoint = $"https://localhost:{_cosmosContainer.GetMappedPublicPort(8081)}/";
            cosmosKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            redisConnectionString = _redisContainer.GetConnectionString();
        }
        else
        {
            //Take env vars injected in dockercompose
            var config = TestConfiguration.FromEnvironment();
            kafkaBootstrapServers = config.Kafka.BootstrapServers;
            cosmosEndpoint = config.CosmosDb.EndpointUri;
            cosmosKey = config.CosmosDb.PrimaryKey;
            redisConnectionString = config.Redis.ConnectionString;
        }

        //Connect to api, kafka and cosmos
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        var connectionString = $"AccountEndpoint={cosmosEndpoint};AccountKey={cosmosKey}";
        _cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
        {
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            })
        });
        _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("IntegrationTestDb");
        _container = await _database.CreateContainerIfNotExistsAsync("transactions", "/id");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ApiSettings:ApiKey", "integration-test-key" },
                        { "CosmosDb:EndpointUri", cosmosEndpoint },
                        { "CosmosDb:PrimaryKey", cosmosKey },
                        { "CosmosDb:DatabaseName", "IntegrationTestDb" },
                        { "CosmosDb:ContainerName", "transactions" },
                        { "CosmosDb:PartitionKeyPath", "/id" },
                        { "Redis:ConnectionString", redisConnectionString },
                        { "Kafka:BootstrapServers", kafkaBootstrapServers },
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
    /// This test verifies the complete end-to-end transaction processing from Kafka message to API retrieval using TestContainers
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

        if (_useTestContainers)
        {
            if (_kafkaContainer != null) await _kafkaContainer.DisposeAsync();
            if (_cosmosContainer != null) await _cosmosContainer.DisposeAsync();
            if (_redisContainer != null) await _redisContainer.DisposeAsync();
        }
    }
}
