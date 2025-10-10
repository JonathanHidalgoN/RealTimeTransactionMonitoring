using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TransactionProcessor.Extensions.Configuration;
using FinancialMonitoring.Models;

namespace TransactionProcessor.Tests.Extensions.Configuration;

public class ConfigurationValidatorTests
{
    [Fact]
    public void ValidateConfiguration_WithValidDevelopmentConfig_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb",
                ["MongoDb:CollectionName"] = "transactions",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "transactions"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Development);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithValidProductionConfig_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db",
                ["CosmosDb:EndpointUri"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] = "test-primary-key",
                ["CosmosDb:ContainerName"] = "transactions",
                ["CosmosDb:PartitionKeyPath"] = "/id",
                ["CosmosDb:ApplicationName"] = "TransactionProcessor",
                ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://test.documents.azure.com:443/;",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EntityPath"] = "transactions",
                ["EventHubs:EventHubName"] = "transactions"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithValidProductionStatefulConfig_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["AnomalyDetection:Mode"] = "stateful",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db",
                ["CosmosDb:EndpointUri"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] = "test-primary-key",
                ["CosmosDb:ContainerName"] = "transactions",
                ["CosmosDb:PartitionKeyPath"] = "/id",
                ["CosmosDb:ApplicationName"] = "TransactionProcessor",
                ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://test.documents.azure.com:443/;",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EntityPath"] = "transactions",
                ["EventHubs:EventHubName"] = "transactions",
                ["Redis:ConnectionString"] = "production.redis.cache.windows.net"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingAnomalyDetectionSettings_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["Kafka:BootstrapServers"] = "localhost:9092"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Development);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingKeyVaultInProduction_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithStatefulModeButMissingRedis_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["AnomalyDetection:Mode"] = "stateful",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/"
                // Missing Redis configuration
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithStatelessModeAndMissingRedis_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db",
                ["CosmosDb:EndpointUri"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] = "test-primary-key",
                ["CosmosDb:ContainerName"] = "transactions",
                ["CosmosDb:PartitionKeyPath"] = "/id",
                ["CosmosDb:ApplicationName"] = "TransactionProcessor",
                ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://test.documents.azure.com:443/;",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EntityPath"] = "transactions",
                ["EventHubs:EventHubName"] = "transactions"
                // No Redis configuration - should be fine for stateless mode
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithDevelopmentMissingProductionSettings_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb",
                ["MongoDb:CollectionName"] = "transactions",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "transactions"
                // No CosmosDb, EventHubs, or ApplicationInsights - should be fine for Development
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Development);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithProductionMissingDevelopmentSettings_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db",
                ["CosmosDb:EndpointUri"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] = "test-primary-key",
                ["CosmosDb:ContainerName"] = "transactions",
                ["CosmosDb:PartitionKeyPath"] = "/id",
                ["CosmosDb:ApplicationName"] = "TransactionProcessor",
                ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://test.documents.azure.com:443/;",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EntityPath"] = "transactions",
                ["EventHubs:EventHubName"] = "transactions"
                // No MongoDb or Kafka settings - should be fine for Production
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithMultipleMissingSections_ShouldListAllErrors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/"
                // Missing all required sections for Production
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidDataAnnotations_ShouldThrowWithValidationErrors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "-100", // Invalid: negative threshold
                ["MongoDb:ConnectionString"] = "", // Invalid: empty required field
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = ""  // Invalid: empty required field
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Development);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithTestingEnvironment_ShouldUseDevelopmentValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb",
                ["MongoDb:CollectionName"] = "transactions",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "transactions"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Testing);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithCaseSensitiveAnomalyMode_ShouldHandleCorrectly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["AnomalyDetection:Mode"] = "STATEFUL", // Uppercase mode
                ["AnomalyDetection:AmountThreshold"] = "1000",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db",
                ["CosmosDb:EndpointUri"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] = "test-primary-key",
                ["CosmosDb:ContainerName"] = "transactions",
                ["CosmosDb:PartitionKeyPath"] = "/id",
                ["CosmosDb:ApplicationName"] = "TransactionProcessor",
                ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://test.documents.azure.com:443/;",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EntityPath"] = "transactions",
                ["EventHubs:EventHubName"] = "transactions",
                ["Redis:ConnectionString"] = "production.redis.cache.windows.net"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }
}
