using Confluent.Kafka;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using System.Text.Json;

namespace FinancialMonitoring.IntegrationTests;

/// <summary>
/// Basic connectivity tests that verify infrastructure services are running
/// </summary>
public class BasicConnectivityTests : IAsyncLifetime
{
    private IProducer<Null, string>? _producer;
    private IMongoClient? _mongoClient;
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

            var testKey = "test:connectivity";
            var testValue = "integration-test";

            await database.StringSetAsync(testKey, testValue);
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
    public async Task MongoDB_ShouldBeReachable()
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable("MongoDb__ConnectionString") ?? "mongodb://admin:password123@mongodb-test:27017";
        var mongoDatabaseName = Environment.GetEnvironmentVariable("MongoDb__DatabaseName") ?? "TestFinancialMonitoring";

        try
        {
            _mongoClient = new MongoClient(mongoConnectionString);
            var database = _mongoClient.GetDatabase(mongoDatabaseName);

            // Test connectivity by creating inserting a document
            var testCollection = database.GetCollection<BsonDocument>("connectivity_test");
            var testDocument = new BsonDocument
            {
                { "test", "connectivity" },
                { "timestamp", DateTime.UtcNow },
                { "source", "integration-test" }
            };

            await testCollection.InsertOneAsync(testDocument);

            // Verify we can read it
            var filter = Builders<BsonDocument>.Filter.Eq("test", "connectivity");
            var retrievedDocument = await testCollection.Find(filter).FirstOrDefaultAsync();
            Assert.NotNull(retrievedDocument);
            Assert.Equal("connectivity", retrievedDocument["test"].AsString);

            await testCollection.DeleteOneAsync(filter);
        }
        catch (Exception ex)
        {
            Assert.Fail($"MongoDB connectivity failed: {ex.Message}");
        }
    }

    [Fact]
    public void Environment_ShouldBeConfiguredForTesting()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        Assert.Equal("Testing", environment);
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        _mongoClient = null;
        _redis?.Dispose();
        await Task.CompletedTask;
    }
}
