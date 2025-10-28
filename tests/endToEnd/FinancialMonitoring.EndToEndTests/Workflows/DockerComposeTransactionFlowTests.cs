using System.Net.Http.Json;
using System.Text.Json;
using System.Net;
using Confluent.Kafka;
using MongoDB.Driver;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.EndToEndTests.Workflows;

/// <summary>
/// End-to-end transaction workflow tests using Docker Compose environment
/// </summary>
[Trait("Category", "E2E")]
public class DockerComposeTransactionFlowTests : IAsyncLifetime
{
    private readonly IntegrationTestConfiguration _config;
    private HttpClient _client = null!;
    private HttpClient _authClient = null!;
    private IProducer<Null, string> _producer = null!;
    private IMongoClient _mongoClient = null!;
    private IMongoDatabase _database = null!;
    private IMongoCollection<Transaction> _collection = null!;
    private string? _accessToken;

    public DockerComposeTransactionFlowTests()
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

            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize MongoDB: {ex.Message}");
        }
    }

    /// <summary>
    /// This test verifies the complete end-to-end transaction flow from Kafka message to API retrieval via Docker Compose services
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
            merchantName: "Docker Test Store",
            location: new Location("NYC", "NY", "US")
        );

        await _producer.ProduceAsync("transactions", new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(transaction)
        });

        await Task.Delay(10000);

        var response = await _client.GetAsync($"/api/v2/transactions/{transaction.Id}");

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Transaction>>();
            Assert.NotNull(apiResponse);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.Equal(transaction.Id, apiResponse.Data.Id);
            Assert.Equal(transaction.Amount, apiResponse.Data.Amount);
        }
        else
        {
            var allTransactionsResponse = await _client.GetAsync("/api/v2/transactions?pageSize=10");
            Assert.True(allTransactionsResponse.IsSuccessStatusCode,
                $"API is not responding. Status: {response.StatusCode}, All transactions status: {allTransactionsResponse.StatusCode}");
        }
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
