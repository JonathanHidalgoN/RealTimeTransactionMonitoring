using Confluent.Kafka;
using Microsoft.Azure.Cosmos;
using StackExchange.Redis;
using System.Text.Json;

namespace FinancialMonitoring.IntegrationTests;

/// <summary>
/// Basic connectivity tests that verify infrastructure services are running
/// </summary>
public class BasicConnectivityTests : IAsyncLifetime
{
    private IProducer<Null, string>? _producer;
    private CosmosClient? _cosmosClient;
    private IConnectionMultiplexer? _redis;

    public async Task InitializeAsync()
    {
        await Task.Delay(15000);
    }

    [Fact]
    public async Task Kafka_ShouldBeReachable()
    {
        //Conect to kafka, this variable is inject in dockercompose file
        var kafkaBootstrapServers = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "kafka:29092";

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            MessageTimeoutMs = 30000,
            RequestTimeoutMs = 30000,
            MetadataMaxAgeMs = 30000,
            SocketTimeoutMs = 30000
        };

        Exception? lastException = null;
        const int maxRetries = 3;
        const int retryDelayMs = 10000;

        //Try 3 times to write to kafka
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _producer = new ProducerBuilder<Null, string>(config).Build();

                var testMessage = new { test = "connectivity", timestamp = DateTimeOffset.UtcNow, attempt };
                var result = await _producer.ProduceAsync("transactions", new Message<Null, string>
                {
                    Value = JsonSerializer.Serialize(testMessage)
                });

                Assert.NotNull(result);
                //Assert that the message is updated
                Assert.Equal(PersistenceStatus.Persisted, result.Status);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _producer?.Dispose();
                _producer = null;

                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs);
                }
            }
        }

        Assert.Fail($"Kafka connectivity failed after {maxRetries} attempts. Last error: {lastException?.Message}");
    }

    [Fact]
    public async Task Redis_ShouldBeReachable()
    {
        //Conect to redis, this variable is inject in dockercompose file
        var redisConnectionString = Environment.GetEnvironmentVariable("Redis__ConnectionString") ?? "redis:6379";

        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            var database = _redis.GetDatabase();

            //Insert dummy value
            var testKey = "test:connectivity";
            var testValue = "integration-test";

            await database.StringSetAsync(testKey, testValue);
            //Retrive and assert dummy value is the same
            var retrievedValue = await database.StringGetAsync(testKey);

            Assert.Equal(testValue, retrievedValue);

            //Delete dummy value
            await database.KeyDeleteAsync(testKey);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Redis connectivity failed: {ex.Message}");
        }
    }

    [Fact]
    public async Task CosmosDB_ShouldBeReachable()
    {
        //Conect to cosmosdb-emulator, this variable is inject in dockercompose file
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__EndpointUri") ?? "https://cosmosdb-emulator:8081";
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__PrimaryKey") ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        try
        {
            //Connect to cosmos
            var connectionString = $"AccountEndpoint={cosmosEndpoint};AccountKey={cosmosKey}";
            _cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
            {
                HttpClientFactory = () => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                })
            });

            var accountProperties = await _cosmosClient.ReadAccountAsync();
            Assert.NotNull(accountProperties);
        }
        catch (Exception ex)
        {
            Assert.Fail($"CosmosDB connectivity failed: {ex.Message}");
        }
    }

    [Fact]
    public void Environment_ShouldBeConfiguredForTesting()
    {
        //Check correct env variable balue for testing
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        Assert.Equal("Testing", environment);
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        _cosmosClient?.Dispose();
        _redis?.Dispose();
        await Task.CompletedTask;
    }
}
