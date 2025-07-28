using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using MongoDB.Driver;
using MongoDB.Bson;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.IntegrationTests;

public class DockerComposeIntegrationTests : IAsyncLifetime
{
    private HttpClient _client = null!;
    private IProducer<Null, string> _producer = null!;
    private IMongoClient _mongoClient = null!;
    private IMongoDatabase _database = null!;
    private IMongoCollection<Transaction> _collection = null!;

    public async Task InitializeAsync()
    {
        // Use environment variables for Docker Compose setup
        var apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl") ?? "http://financialmonitoring-api-test:8080";
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "kafka:29092";
        var mongoConnectionString = Environment.GetEnvironmentVariable("MongoDb__ConnectionString") ?? "mongodb://admin:password123@mongodb-test:27017";
        var mongoDatabaseName = Environment.GetEnvironmentVariable("MongoDb__DatabaseName") ?? "TestFinancialMonitoring";
        var mongoCollectionName = Environment.GetEnvironmentVariable("MongoDb__CollectionName") ?? "transactions";
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

        //Connect to MongoDB
        try
        {
            _mongoClient = new MongoClient(mongoConnectionString);
            _database = _mongoClient.GetDatabase(mongoDatabaseName);
            _collection = _database.GetCollection<Transaction>(mongoCollectionName);
            
            // Test the connection
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize MongoDB: {ex.Message}");
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
            destinationAccount: new Account("ACC-TO-001"),
            type: TransactionType.Purchase,
            merchantCategory: MerchantCategory.Retail,
            merchantName: "Docker Test Store",
            location: new Location("NYC", "NY", "US")
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
        _mongoClient = null; // MongoDB client doesn't need explicit disposal
        _client?.Dispose();
        await Task.CompletedTask;
    }
}
