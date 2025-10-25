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
                ["CacheSettings:ConnectionString"] ="localhost:6379",
                ["ResponseCacheSettings:DefaultDuration"] ="300",
                ["RateLimitSettings:PermitLimit"] ="100",
                ["JwtSettings:SecretKey"] ="super-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] ="test-issuer",
                ["JwtSettings:Audience"] ="test-audience",
                ["Cors:AllowedOrigins:0"] ="http://localhost:3000",
                ["MongoDb:ConnectionString"] ="mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] ="testdb",
                ["MongoDb:CollectionName"] ="transactions"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

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
                ["CacheSettings:ConnectionString"] ="production.redis.cache.windows.net",
                ["ResponseCacheSettings:DefaultDuration"] ="300",
                ["RateLimitSettings:PermitLimit"] ="100",
                ["JwtSettings:SecretKey"] ="super-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] ="production-issuer",
                ["JwtSettings:Audience"] ="production-audience",
                ["Cors:AllowedOrigins:0"] ="https://myapp.com",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] ="https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] ="production-db",
                ["CosmosDb:ApplicationName"] ="FinancialMonitoring",
                ["CosmosDb:ConnectionString"] ="AccountEndpoint=https://test.documents.azure.com:443/;",
                ["CosmosDb:EndpointUri"] ="https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] ="test-primary-key-value",
                ["CosmosDb:ContainerName"] ="transactions",
                ["CosmosDb:PartitionKeyPath"] ="/id"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingCacheSettings_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["MongoDb:ConnectionString"] ="mongodb://localhost:27017"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMissingKeyVaultInProduction_ShouldThrowWithSpecificError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Production",
                ["CacheSettings:ConnectionString"] ="production.redis.cache.windows.net",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] ="https://test.documents.azure.com:443/"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

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
                ["CacheSettings:ConnectionString"] ="production.redis.cache.windows.net"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidDataAnnotations_ShouldThrowWithValidationErrors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["CacheSettings:ConnectionString"] ="localhost:6379",
                ["RateLimitSettings:PermitLimit"] ="-1", // Invalid: negative number
                ["JwtSettings:SecretKey"] ="short", // Invalid: too short
                ["MongoDb:ConnectionString"] ="mongodb://localhost:27017"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithMultipleMissingSections_ShouldListAllErrors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development"
                // Missing Cache, RateLimit, Jwt, Cors, MongoDb sections
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateConfiguration_WithDevelopmentMissingCosmosSettings_ShouldNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Development",
                ["CacheSettings:ConnectionString"] ="localhost:6379",
                ["ResponseCacheSettings:DefaultDuration"] ="300",
                ["RateLimitSettings:PermitLimit"] ="100",
                ["JwtSettings:SecretKey"] ="super-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] ="test-issuer",
                ["JwtSettings:Audience"] ="test-audience",
                ["Cors:AllowedOrigins:0"] ="http://localhost:3000",
                ["MongoDb:ConnectionString"] ="mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] ="testdb",
                ["MongoDb:CollectionName"] ="transactions"
                // No CosmosDb or ApplicationInsights - should be fine for Development
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

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
                ["CacheSettings:ConnectionString"] ="production.redis.cache.windows.net",
                ["ResponseCacheSettings:DefaultDuration"] ="300",
                ["RateLimitSettings:PermitLimit"] ="100",
                ["JwtSettings:SecretKey"] ="super-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] ="production-issuer",
                ["JwtSettings:Audience"] ="production-audience",
                ["Cors:AllowedOrigins:0"] ="https://myapp.com",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:AccountEndpoint"] ="https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] ="production-db",
                ["CosmosDb:ApplicationName"] ="FinancialMonitoring",
                ["CosmosDb:ConnectionString"] ="AccountEndpoint=https://test.documents.azure.com:443/;",
                ["CosmosDb:EndpointUri"] ="https://test.documents.azure.com:443/",
                ["CosmosDb:PrimaryKey"] ="test-primary-key-value",
                ["CosmosDb:ContainerName"] ="transactions",
                ["CosmosDb:PartitionKeyPath"] ="/id"
                // No MongoDb settings - should be fine for Production
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithTestingEnvironment_ShouldUseDevelopmentValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AppConstants.runTimeEnvVarName] = "Testing",
                ["CacheSettings:ConnectionString"] ="localhost:6379",
                ["ResponseCacheSettings:DefaultDuration"] ="300",
                ["RateLimitSettings:PermitLimit"] ="100",
                ["JwtSettings:SecretKey"] ="super-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] ="test-issuer",
                ["JwtSettings:Audience"] ="test-audience",
                ["Cors:AllowedOrigins:0"] ="http://localhost:3000",
                ["MongoDb:ConnectionString"] ="mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] ="testdb",
                ["MongoDb:CollectionName"] ="transactions"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithNullEnvironment_ShouldDefaultToDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // No environment variable set - should default to Development
                ["CacheSettings:ConnectionString"] ="localhost:6379",
                ["ResponseCacheSettings:DefaultDuration"] ="300",
                ["RateLimitSettings:PermitLimit"] ="100",
                ["JwtSettings:SecretKey"] ="super-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] ="test-issuer",
                ["JwtSettings:Audience"] ="test-audience",
                ["Cors:AllowedOrigins:0"] ="http://localhost:3000",
                ["MongoDb:ConnectionString"] ="mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] ="testdb",
                ["MongoDb:CollectionName"] ="transactions"
            })
            .Build();

        var environmentString = configuration[AppConstants.runTimeEnvVarName] ?? "Development";
        var environment = RunTimeEnvironmentExtensions.FromString(environmentString);
        var action = () => ConfigurationValidator.ValidateConfiguration(configuration, environment);

        action.Should().NotThrow();
    }
}
