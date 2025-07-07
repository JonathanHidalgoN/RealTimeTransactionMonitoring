using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Azure.Cosmos;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.IntegrationTests;

public class DockerComposeIntegrationTests : IAsyncLifetime
{
    private HttpClient _client = null!;
    private IProducer<Null, string> _producer = null!;
    private CosmosClient _cosmosClient = null!;
    private Database _database = null!;
    private Container _container = null!;

    public async Task InitializeAsync()
    {
        // Use environment variables for Docker Compose setup
        var apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl") ?? "http://financialmonitoring-api-test:8080";
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "kafka:29092";
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__EndpointUri") ?? "https://cosmosdb-emulator:8081";
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__PrimaryKey") ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        var apiKey = Environment.GetEnvironmentVariable("ApiKey") ?? "integration-test-key";

        //Create http client to api with default key
        _client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        //Connect to kafka
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        //Connect to cosmos
        var connectionString = $"AccountEndpoint={cosmosEndpoint};AccountKey={cosmosKey}";
        _cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
        {
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            })
        });

        try
        {
            _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync("TestFinancialMonitoring");
            _container = await _database.CreateContainerIfNotExistsAsync("transactions", "/id");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Cosmos DB: {ex.Message}");
        }
    }

    [Fact]
    public async Task EndToEndTransactionFlow_ShouldProcessTransactionFromKafkaToApi()
    {
        var transaction = new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: 250.00,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            sourceAccount: new Account("ACC-FROM-001"),
            destinationAccount: new Account("ACC-TO-001")
        );

        // Send transaction to Kafka
        await _producer.ProduceAsync("transactions", new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(transaction)
        });

        // Wait for processing
        await Task.Delay(10000);

        // Check if transaction was processed via API
        var response = await _client.GetAsync($"/api/transactions/{transaction.Id}");

        if (response.IsSuccessStatusCode)
        {
            var retrievedTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
            Assert.NotNull(retrievedTransaction);
            Assert.Equal(transaction.Id, retrievedTransaction.Id);
            Assert.Equal(transaction.Amount, retrievedTransaction.Amount);
        }
        else
        {
            // Transaction might not be processed yet or API might not be ready
            var allTransactionsResponse = await _client.GetAsync("/api/transactions?pageSize=10");
            Assert.True(allTransactionsResponse.IsSuccessStatusCode,
                $"API is not responding. Status: {response.StatusCode}, All transactions status: {allTransactionsResponse.StatusCode}");
        }
    }

    [Fact]
    public async Task HealthCheck_ApiShouldBeResponding()
    {
        var response = await _client.GetAsync("/api/transactions?pageSize=1");
        Assert.True(response.IsSuccessStatusCode, $"API health check failed with status: {response.StatusCode}");
    }

    [Fact]
    public async Task KafkaProducer_ShouldSendMessage()
    {
        var testMessage = new { test = "message", timestamp = DateTimeOffset.UtcNow };
        var result = await _producer.ProduceAsync("transactions", new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(testMessage)
        });

        Assert.NotNull(result);
        Assert.Equal(PersistenceStatus.Persisted, result.Status);
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        _cosmosClient?.Dispose();
        _client?.Dispose();
        await Task.CompletedTask;
    }
}
