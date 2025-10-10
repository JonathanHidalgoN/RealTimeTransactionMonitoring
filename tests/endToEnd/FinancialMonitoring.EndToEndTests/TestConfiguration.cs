using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.EndToEndTests;

/// <summary>
/// Centralized configuration management for end-to-end tests
/// Eliminates scattered Environment.GetEnvironmentVariable calls and provides validation
/// </summary>
public class IntegrationTestConfiguration
{
    public ApiConfiguration Api { get; set; } = new();
    public KafkaConfiguration Kafka { get; set; } = new();
    public MongoDbConfiguration MongoDb { get; set; } = new();
    public RedisConfiguration Redis { get; set; } = new();
    public CosmosDbConfiguration CosmosDb { get; set; } = new();
    public OAuth2Configuration OAuth2 { get; set; } = new();
    public EnvironmentConfiguration Environment { get; set; } = new();
    public ConnectivityTestConfiguration ConnectivityTest { get; set; } = new();

    /// <summary>
    /// Creates an IntegrationTestConfiguration from environment variables with fallback defaults
    /// </summary>
    public static IntegrationTestConfiguration FromEnvironment()
    {
        return new IntegrationTestConfiguration
        {
            Api = new ApiConfiguration
            {
                BaseUrl = GetEnvVar("ApiBaseUrl", "http://financialmonitoring-api-test:8080"),
                ApiKey = GetEnvVar("ApiKey", "integration-test-key")
            },
            Kafka = new KafkaConfiguration
            {
                BootstrapServers = GetEnvVar("Kafka__BootstrapServers", "kafka:29092")
            },
            MongoDb = new MongoDbConfiguration
            {
                ConnectionString = GetEnvVar("MongoDb__ConnectionString", "mongodb://admin:password123@mongodb-test:27017"),
                DatabaseName = GetEnvVar("MongoDb__DatabaseName", "TestFinancialMonitoring"),
                CollectionName = GetEnvVar("MongoDb__CollectionName", "transactions")
            },
            Redis = new RedisConfiguration
            {
                ConnectionString = GetEnvVar("Redis__ConnectionString", "redis:6379")
            },
            CosmosDb = new CosmosDbConfiguration
            {
                EndpointUri = GetEnvVar("CosmosDb__EndpointUri", "https://cosmosdb-emulator:8081"),
                PrimaryKey = GetEnvVar("CosmosDb__PrimaryKey", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==")
            },
            OAuth2 = new OAuth2Configuration
            {
                ClientId = GetEnvVar("OAuth_ClientId", "test-client"),
                ClientSecret = GetEnvVar("OAuth_ClientSecret", "test-secret")
            },
            Environment = new EnvironmentConfiguration
            {
                DotNetEnvironment = GetEnvVar("DOTNET_ENVIRONMENT", "Development"),
                IsTestingEnvironment = GetEnvVar("DOTNET_ENVIRONMENT", "Development") == "Testing"
            },
            ConnectivityTest = new ConnectivityTestConfiguration()
        };
    }

    /// <summary>
    /// Fluent configuration builder for test customization
    /// </summary>
    public IntegrationTestConfiguration WithApiUrl(string baseUrl)
    {
        Api.BaseUrl = baseUrl;
        return this;
    }

    public IntegrationTestConfiguration WithApiKey(string apiKey)
    {
        Api.ApiKey = apiKey;
        return this;
    }

    public IntegrationTestConfiguration WithKafka(string bootstrapServers)
    {
        Kafka.BootstrapServers = bootstrapServers;
        return this;
    }

    public IntegrationTestConfiguration WithMongo(string connectionString, string? databaseName = null, string? collectionName = null)
    {
        MongoDb.ConnectionString = connectionString;
        if (databaseName != null) MongoDb.DatabaseName = databaseName;
        if (collectionName != null) MongoDb.CollectionName = collectionName;
        return this;
    }

    public IntegrationTestConfiguration WithRedis(string connectionString)
    {
        Redis.ConnectionString = connectionString;
        return this;
    }

    public IntegrationTestConfiguration WithCosmosDb(string endpointUri, string primaryKey)
    {
        CosmosDb.EndpointUri = endpointUri;
        CosmosDb.PrimaryKey = primaryKey;
        return this;
    }

    public IntegrationTestConfiguration WithOAuth2(string clientId, string clientSecret)
    {
        OAuth2.ClientId = clientId;
        OAuth2.ClientSecret = clientSecret;
        return this;
    }

    public IntegrationTestConfiguration WithConnectivityTestSettings(int initDelayMs = 15000, int kafkaTimeoutMs = 30000)
    {
        ConnectivityTest.InitializationDelayMs = initDelayMs;
        ConnectivityTest.KafkaMessageTimeoutMs = kafkaTimeoutMs;
        ConnectivityTest.KafkaRequestTimeoutMs = kafkaTimeoutMs;
        ConnectivityTest.KafkaMetadataMaxAgeMs = kafkaTimeoutMs;
        ConnectivityTest.KafkaSocketTimeoutMs = kafkaTimeoutMs;
        return this;
    }

    /// <summary>
    /// Validates all configuration values
    /// </summary>
    public void Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();

        //This will validate metadata annotation on config classes
        if (!Validator.TryValidateObject(this, context, results, true))
        {
            var errors = string.Join(", ", results.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Invalid test configuration: {errors}");
        }

        ValidateUrls();
        ValidateConnectionStrings();
    }

    private void ValidateUrls()
    {
        if (!Uri.TryCreate(Api.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"Invalid API base URL: {Api.BaseUrl}");

        if (!Uri.TryCreate(CosmosDb.EndpointUri, UriKind.Absolute, out _))
            throw new InvalidOperationException($"Invalid CosmosDB endpoint URI: {CosmosDb.EndpointUri}");
    }

    private void ValidateConnectionStrings()
    {
        if (string.IsNullOrWhiteSpace(MongoDb.ConnectionString))
            throw new InvalidOperationException("MongoDB connection string is required");

        if (string.IsNullOrWhiteSpace(Redis.ConnectionString))
            throw new InvalidOperationException("Redis connection string is required");
    }

    private static string GetEnvVar(string name, string defaultValue)
    {
        return System.Environment.GetEnvironmentVariable(name) ?? defaultValue;
    }
}

public class ApiConfiguration
{
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}

public class KafkaConfiguration
{
    [Required]
    public string BootstrapServers { get; set; } = string.Empty;
}

public class MongoDbConfiguration
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string CollectionName { get; set; } = string.Empty;
}

public class RedisConfiguration
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}

public class CosmosDbConfiguration
{
    [Required]
    public string EndpointUri { get; set; } = string.Empty;

    [Required]
    public string PrimaryKey { get; set; } = string.Empty;
}

public class OAuth2Configuration
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}

public class EnvironmentConfiguration
{
    public string DotNetEnvironment { get; set; } = string.Empty;
    public bool IsTestingEnvironment { get; set; }
}

public class ConnectivityTestConfiguration
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
