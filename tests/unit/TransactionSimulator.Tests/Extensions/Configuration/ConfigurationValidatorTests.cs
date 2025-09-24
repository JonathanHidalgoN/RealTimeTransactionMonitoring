using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TransactionSimulator.Extensions.Configuration;
using FinancialMonitoring.Models;

namespace TransactionSimulator.Tests.Extensions.Configuration;

public class ConfigurationValidatorTests
{
    [Fact]
    public void ValidateConfiguration_WithValidDevelopmentConfig_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "transactions",
                ["Kafka:TimeoutSeconds"] = "30"
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
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EventHubName"] = "transactions",
                ["EventHubs:BlobStorageConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;",
                ["EventHubs:BlobContainerName"] = "checkpoints",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingKafkaInDevelopment_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Missing Kafka configuration
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Development);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingKeyVaultInProduction_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EventHubName"] = "transactions",
                ["EventHubs:BlobStorageConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;",
                ["EventHubs:BlobContainerName"] = "checkpoints",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key"
                // Missing KEY_VAULT_URI
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithEmptyKeyVaultInProduction_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EventHubName"] = "transactions",
                ["EventHubs:BlobStorageConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;",
                ["EventHubs:BlobContainerName"] = "checkpoints",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingEventHubsInProduction_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key"
                // Missing EventHubs configuration
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingApplicationInsightsInProduction_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EntityPath"] = "transactions"
                // Missing ApplicationInsights configuration
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidKafkaConfiguration_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "", // Invalid: empty required field
                ["Kafka:Topic"] = "transactions",
                ["Kafka:TimeoutSeconds"] = "30"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Development);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidApplicationInsightsConfiguration_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EventHubName"] = "transactions",
                ["EventHubs:BlobStorageConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;",
                ["EventHubs:BlobContainerName"] = "checkpoints",
                ["ApplicationInsights:ConnectionString"] = "" // Invalid: empty required field
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithTestingEnvironment_ShouldUseDevelopmentValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "transactions",
                ["Kafka:TimeoutSeconds"] = "30"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Testing);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithDevelopmentMissingProductionSettings_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "transactions",
                ["Kafka:TimeoutSeconds"] = "30"
                // No EventHubs or ApplicationInsights - should be fine for Development
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
                ["EventHubs:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/",
                ["EventHubs:EventHubName"] = "transactions",
                ["EventHubs:BlobStorageConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;",
                ["EventHubs:BlobContainerName"] = "checkpoints",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key"
                // No Kafka settings - should be fine for Production
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, RunTimeEnvironment.Production);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithMultipleMissingSections_ShouldThrow()
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
}
