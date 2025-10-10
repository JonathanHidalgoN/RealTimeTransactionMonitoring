using Confluent.Kafka;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using System.Text.Json;

namespace FinancialMonitoring.EndToEndTests.Infrastructure;


/// <summary>
/// Basic connectivity tests that verify infrastructure services are running
/// </summary>
[Trait("Category", "Infrastructure")]
public class BasicConnectivityTests : IAsyncLifetime
{
    private readonly IntegrationTestConfiguration _config;
    private IProducer<Null, string>? _producer;
    private IMongoClient? _mongoClient;
    private IConnectionMultiplexer? _redis;

    public BasicConnectivityTests()
    {
        _config = IntegrationTestConfiguration.FromEnvironment();
        _config.Validate();
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(_config.ConnectivityTest.InitializationDelayMs);
    }

    /// <summary>
    /// This test verifies that Kafka is reachable and can successfully send and persist messages
    /// </summary>
    [Fact]
    public async Task Kafka_ShouldBeReachable()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _config.Kafka.BootstrapServers,
            MessageTimeoutMs = _config.ConnectivityTest.KafkaMessageTimeoutMs,
            RequestTimeoutMs = _config.ConnectivityTest.KafkaRequestTimeoutMs,
            MetadataMaxAgeMs = _config.ConnectivityTest.KafkaMetadataMaxAgeMs,
            SocketTimeoutMs = _config.ConnectivityTest.KafkaSocketTimeoutMs
        };

        Exception? lastException = null;

        //Try multiple times to write to kafka
        for (int attempt = 1; attempt <= _config.ConnectivityTest.KafkaMaxRetries; attempt++)
        {
            try
            {
                _producer = new ProducerBuilder<Null, string>(config).Build();

                var testMessage = new { test = "connectivity", timestamp = DateTimeOffset.UtcNow, attempt };
                var result = await _producer.ProduceAsync(_config.ConnectivityTest.KafkaTopicName, new Message<Null, string>
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

                if (attempt < _config.ConnectivityTest.KafkaMaxRetries)
                {
                    await Task.Delay(_config.ConnectivityTest.KafkaRetryDelayMs);
                }
            }
        }

        Assert.Fail($"Kafka connectivity failed after {_config.ConnectivityTest.KafkaMaxRetries} attempts. Last error: {lastException?.Message}");
    }

    /// <summary>
    /// This test verifies that Redis is reachable and can successfully store and retrieve data
    /// </summary>
    [Fact]
    public async Task Redis_ShouldBeReachable()
    {
        try
        {
            _redis = await ConnectionMultiplexer.ConnectAsync(_config.Redis.ConnectionString);
            var database = _redis.GetDatabase();

            await database.StringSetAsync(_config.ConnectivityTest.RedisTestKeyPrefix, _config.ConnectivityTest.RedisTestValue);
            var retrievedValue = await database.StringGetAsync(_config.ConnectivityTest.RedisTestKeyPrefix);

            Assert.Equal(_config.ConnectivityTest.RedisTestValue, retrievedValue);

            //Delete dummy value
            await database.KeyDeleteAsync(_config.ConnectivityTest.RedisTestKeyPrefix);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Redis connectivity failed: {ex.Message}");
        }
    }

    /// <summary>
    /// This test verifies that MongoDB is reachable and can successfully insert and query documents
    /// </summary>
    [Fact]
    public async Task MongoDB_ShouldBeReachable()
    {
        try
        {
            _mongoClient = new MongoClient(_config.MongoDb.ConnectionString);
            var database = _mongoClient.GetDatabase(_config.MongoDb.DatabaseName);

            // Test connectivity by creating inserting a document
            var testCollection = database.GetCollection<BsonDocument>(_config.ConnectivityTest.MongoDbTestCollectionName);
            var testDocument = new BsonDocument
            {
                { _config.ConnectivityTest.MongoDbTestFieldName, _config.ConnectivityTest.MongoDbTestFieldValue },
                { "timestamp", DateTime.UtcNow },
                { "source", _config.ConnectivityTest.MongoDbTestSource }
            };

            await testCollection.InsertOneAsync(testDocument);

            // Verify we can read it
            var filter = Builders<BsonDocument>.Filter.Eq(_config.ConnectivityTest.MongoDbTestFieldName, _config.ConnectivityTest.MongoDbTestFieldValue);
            var retrievedDocument = await testCollection.Find(filter).FirstOrDefaultAsync();
            Assert.NotNull(retrievedDocument);
            Assert.Equal(_config.ConnectivityTest.MongoDbTestFieldValue, retrievedDocument[_config.ConnectivityTest.MongoDbTestFieldName].AsString);

            await testCollection.DeleteOneAsync(filter);
        }
        catch (Exception ex)
        {
            Assert.Fail($"MongoDB connectivity failed: {ex.Message}");
        }
    }

    /// <summary>
    /// This test verifies that the environment is properly configured with DOTNET_ENVIRONMENT set to Testing
    /// </summary>
    [Fact]
    [Trait("Category", "Smoke")]
    public void Environment_ShouldBeConfiguredForTesting()
    {
        Assert.Equal("Testing", _config.Environment.DotNetEnvironment);
        Assert.True(_config.Environment.IsTestingEnvironment);
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        _mongoClient = null;
        _redis?.Dispose();
        await Task.CompletedTask;
    }
}
