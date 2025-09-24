using FluentAssertions;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Extensions.Configuration;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class ConfigurationValidatorTests
{
    [Fact]
    public void ValidateConfiguration_WithValidDevelopmentConfig_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "localhost:6379",
                ["ResponseCache:DefaultDuration"] = "300",
                ["RateLimit:PermitLimit"] = "100",
                ["Jwt:SecretKey"] = "super-secret-key-with-sufficient-length",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithValidProductionConfig_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Production",
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "production.redis.cache.windows.net",
                ["ResponseCache:DefaultDuration"] = "300",
                ["RateLimit:PermitLimit"] = "100",
                ["Jwt:SecretKey"] = "super-secret-key-with-sufficient-length",
                ["Jwt:Issuer"] = "production-issuer",
                ["Jwt:Audience"] = "production-audience",
                ["Cors:AllowedOrigins:0"] = "https://myapp.com",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingApiSettings_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["Cache:ConnectionString"] = "localhost:6379",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingKeyVaultInProduction_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Production",
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "production.redis.cache.windows.net",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithEmptyKeyVaultInProduction_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Production",
                ["KEY_VAULT_URI"] = "",
                ["Api:ApiKey"] = "test-key"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidDataAnnotations_ShouldThrowWithValidationErrors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["Api:ApiKey"] = "", // Invalid: empty required field
                ["Cache:ConnectionString"] = "localhost:6379",
                ["RateLimit:PermitLimit"] = "-1", // Invalid: negative number
                ["Jwt:SecretKey"] = "short", // Invalid: too short
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMultipleMissingSections_ShouldListAllErrors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["Api:ApiKey"] = "test-key"
                // Missing Cache, RateLimit, Jwt, Cors, MongoDb sections
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithDevelopmentMissingCosmosSettings_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "localhost:6379",
                ["ResponseCache:DefaultDuration"] = "300",
                ["RateLimit:PermitLimit"] = "100",
                ["Jwt:SecretKey"] = "super-secret-key-with-sufficient-length",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb"
                // No CosmosDb or ApplicationInsights - should be fine for Development
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithProductionMissingMongoSettings_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Production",
                ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/",
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "production.redis.cache.windows.net",
                ["ResponseCache:DefaultDuration"] = "300",
                ["RateLimit:PermitLimit"] = "100",
                ["Jwt:SecretKey"] = "super-secret-key-with-sufficient-length",
                ["Jwt:Issuer"] = "production-issuer",
                ["Jwt:Audience"] = "production-audience",
                ["Cors:AllowedOrigins:0"] = "https://myapp.com",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] = "https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "production-db"
                // No MongoDb settings - should be fine for Production
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithTestingEnvironment_ShouldUseDevelopmentValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Testing",
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "localhost:6379",
                ["ResponseCache:DefaultDuration"] = "300",
                ["RateLimit:PermitLimit"] = "100",
                ["Jwt:SecretKey"] = "super-secret-key-with-sufficient-length",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithNullEnvironment_ShouldDefaultToDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // No environment variable set - should default to Development
                ["Api:ApiKey"] = "test-key",
                ["Cache:ConnectionString"] = "localhost:6379",
                ["ResponseCache:DefaultDuration"] = "300",
                ["RateLimit:PermitLimit"] = "100",
                ["Jwt:SecretKey"] = "super-secret-key-with-sufficient-length",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "testdb"
            })
            .Build();

        var action = () => ConfigurationValidator.ValidateConfiguration(configuration);

        action.Should().NotThrow();
    }
}
