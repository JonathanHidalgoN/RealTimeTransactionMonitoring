using Confluent.Kafka;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using System.Text.Json;

namespace FinancialMonitoring.IntegrationTests.Infrastructure;

/// <summary>
/// Configuration for BasicConnectivityTests with timeout and retry settings
/// </summary>
public class BasicConnectivityTestsConfig
{
    public int InitializationDelayMs { get; set; } = 15000;
    
    // Kafka configuration
    public int KafkaMessageTimeoutMs { get; set; } = 30000;
    public int KafkaRequestTimeoutMs { get; set; } = 30000;
    public int KafkaMetadataMaxAgeMs { get; set; } = 30000;
    public int KafkaSocketTimeoutMs { get; set; } = 30000;
    public int KafkaMaxRetries { get; set; } = 3;
    public int KafkaRetryDelayMs { get; set; } = 10000;
    public string KafkaTopicName { get; set; } = "transactions";
    
    // Redis configuration
    public string RedisTestKeyPrefix { get; set; } = "test:connectivity";
    public string RedisTestValue { get; set; } = "integration-test";
    
    // MongoDB configuration
    public string MongoDbTestCollectionName { get; set; } = "connectivity_test";
    public string MongoDbTestFieldName { get; set; } = "test";
    public string MongoDbTestFieldValue { get; set; } = "connectivity";
    public string MongoDbTestSource { get; set; } = "integration-test";
}

/// <summary>
/// Basic connectivity tests that verify infrastructure services are running
/// </summary>
[Trait("Category", "Infrastructure")]
public class BasicConnectivityTests : IAsyncLifetime
{
    private readonly TestConfiguration _config;
    private readonly BasicConnectivityTestsConfig _testConfig;
    private IProducer<Null, string>? _producer;
    private IMongoClient? _mongoClient;
    private IConnectionMultiplexer? _redis;

    public BasicConnectivityTests()
    {
        _config = TestConfiguration.FromEnvironment();
        _config.Validate();
        _testConfig = new BasicConnectivityTestsConfig();
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(_testConfig.InitializationDelayMs);
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
            MessageTimeoutMs = _testConfig.KafkaMessageTimeoutMs,
            RequestTimeoutMs = _testConfig.KafkaRequestTimeoutMs,
            MetadataMaxAgeMs = _testConfig.KafkaMetadataMaxAgeMs,
            SocketTimeoutMs = _testConfig.KafkaSocketTimeoutMs
        };

        Exception? lastException = null;

        //Try multiple times to write to kafka
        for (int attempt = 1; attempt <= _testConfig.KafkaMaxRetries; attempt++)
        {
            try
            {
                _producer = new ProducerBuilder<Null, string>(config).Build();

                var testMessage = new { test = "connectivity", timestamp = DateTimeOffset.UtcNow, attempt };
                var result = await _producer.ProduceAsync(_testConfig.KafkaTopicName, new Message<Null, string>
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

                if (attempt < _testConfig.KafkaMaxRetries)
                {
                    await Task.Delay(_testConfig.KafkaRetryDelayMs);
                }
            }
        }

        Assert.Fail($"Kafka connectivity failed after {_testConfig.KafkaMaxRetries} attempts. Last error: {lastException?.Message}");
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

            await database.StringSetAsync(_testConfig.RedisTestKeyPrefix, _testConfig.RedisTestValue);
            var retrievedValue = await database.StringGetAsync(_testConfig.RedisTestKeyPrefix);

            Assert.Equal(_testConfig.RedisTestValue, retrievedValue);

            //Delete dummy value
            await database.KeyDeleteAsync(_testConfig.RedisTestKeyPrefix);
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
            var testCollection = database.GetCollection<BsonDocument>(_testConfig.MongoDbTestCollectionName);
            var testDocument = new BsonDocument
            {
                { _testConfig.MongoDbTestFieldName, _testConfig.MongoDbTestFieldValue },
                { "timestamp", DateTime.UtcNow },
                { "source", _testConfig.MongoDbTestSource }
            };

            await testCollection.InsertOneAsync(testDocument);

            // Verify we can read it
            var filter = Builders<BsonDocument>.Filter.Eq(_testConfig.MongoDbTestFieldName, _testConfig.MongoDbTestFieldValue);
            var retrievedDocument = await testCollection.Find(filter).FirstOrDefaultAsync();
            Assert.NotNull(retrievedDocument);
            Assert.Equal(_testConfig.MongoDbTestFieldValue, retrievedDocument[_testConfig.MongoDbTestFieldName].AsString);

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
