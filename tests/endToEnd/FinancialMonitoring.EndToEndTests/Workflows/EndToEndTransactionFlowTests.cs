using System.Net.Http.Json;
using System.Text.Json;
using System.Net;
using Confluent.Kafka;
using MongoDB.Driver;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.EndToEndTests.Workflows;

[Trait("Category", "E2E")]
public class EndToEndTransactionFlowTests : IAsyncLifetime
{
    private readonly IntegrationTestConfiguration _config;
    private HttpClient _client = null!;
    private HttpClient _authClient = null!;
    private IProducer<Null, string> _producer = null!;
    private IMongoClient _mongoClient = null!;
    private IMongoDatabase _database = null!;
    private IMongoCollection<Transaction> _collection = null!;
    private string? _accessToken;

    public EndToEndTransactionFlowTests()
    {
        _config = IntegrationTestConfiguration.FromEnvironment();
        _config.Validate();
    }

    public async Task InitializeAsync()
    {
        _client = new HttpClient { BaseAddress = new Uri(_config.Api.BaseUrl) };
        _authClient = new HttpClient { BaseAddress = new Uri(_config.Api.BaseUrl) };

        // Get OAuth token
        try
        {
            _accessToken = await GetOAuthTokenAsync();
            if (!string.IsNullOrEmpty(_accessToken))
            {
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not acquire OAuth token: {ex.Message}");
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config.Kafka.BootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        try
        {
            _mongoClient = new MongoClient(_config.MongoDb.ConnectionString);
            _database = _mongoClient.GetDatabase(_config.MongoDb.DatabaseName);
            _collection = _database.GetCollection<Transaction>(_config.MongoDb.CollectionName);

            // Test the connection
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize MongoDB: {ex.Message}");
        }
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
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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

        await Task.Delay(15000);

        var response = await _client.GetAsync($"/api/v2/transactions/{transaction.Id}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"Expected success status code, but got {response.StatusCode}. Error: {errorContent}");
        }

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Transaction>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(transaction.Id, apiResponse.Data.Id);
        Assert.Equal(transaction.Amount, apiResponse.Data.Amount);
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
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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

        await Task.Delay(15000);

        var response = await _client.GetAsync($"/api/v2/transactions/{anomalousTransaction.Id}");
        Assert.True(response.IsSuccessStatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Transaction>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(anomalousTransaction.Id, apiResponse.Data.Id);
        Assert.NotNull(apiResponse.Data.AnomalyFlag);
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
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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
        await Task.Delay(20000);

        var response = await _client.GetAsync("/api/v2/transactions?pageSize=20");
        Assert.True(response.IsSuccessStatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<Transaction>>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.True(apiResponse.Data.TotalCount >= 10, $"Expected at least 10 transactions, but got {apiResponse.Data.TotalCount}");
    }

    private async Task<string?> GetOAuthTokenAsync()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _config.OAuth2.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.OAuth2.ClientSecret),
            new KeyValuePair<string, string>("scope", "read write")
        });

        var response = await _authClient.PostAsync("/api/v2/oauth/token", formData);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"OAuth token request failed. Status: {response.StatusCode}, Content: {errorContent}");
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

        if (tokenResponse.TryGetProperty("access_token", out var accessToken))
        {
            return accessToken.GetString();
        }

        return null;
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        _mongoClient = null!; // MongoDB client doesn't need explicit disposal
        _client?.Dispose();
        _authClient?.Dispose();
        await Task.CompletedTask;
    }
}
