using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TransactionProcessor.Extensions.ServiceRegistration;
using FinancialMonitoring.Abstractions.Caching;
using FinancialMonitoring.Abstractions.Services;
using FinancialMonitoring.Models;
using TransactionProcessor.AnomalyDetection;
using TransactionProcessor.Caching;

namespace TransactionProcessor.Tests.Extensions.ServiceRegistration;

public class AnomalyDetectionExtensionsTests
{
    [Fact]
    public void AddAnomalyDetection_WithStatelessMode_ShouldRegisterStatelessDetector()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0"
            })
            .Build();

        var result = services.AddAnomalyDetection(configuration);

        result.Should().BeSameAs(services);

        var serviceProvider = services.BuildServiceProvider();
        var anomalyDetector = serviceProvider.GetService<ITransactionAnomalyDetector>();
        anomalyDetector.Should().NotBeNull();
        anomalyDetector.Should().BeOfType<AnomalyDetector>();

        var redisService = serviceProvider.GetService<IRedisCacheService>();
        redisService.Should().BeNull();
    }

    [Fact]
    public void AddAnomalyDetection_WithStatefulMode_ShouldRegisterStatefulDetectorAndRedis()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateful",
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0",
                ["Redis:ConnectionString"] = "localhost:6379"
            })
            .Build();

        var result = services.AddAnomalyDetection(configuration);

        result.Should().BeSameAs(services);

        var serviceProvider = services.BuildServiceProvider();
        var anomalyDetector = serviceProvider.GetService<ITransactionAnomalyDetector>();
        anomalyDetector.Should().NotBeNull();
        anomalyDetector.Should().BeOfType<StatefulAnomalyDetector>();

        var redisService = serviceProvider.GetService<IRedisCacheService>();
        redisService.Should().NotBeNull();
        redisService.Should().BeOfType<RedisCacheService>();
    }

    [Fact]
    public void AddAnomalyDetection_WithNoModeSpecified_ShouldDefaultToStateless()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0"
                // No Mode specified - should default to stateless
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var anomalyDetector = serviceProvider.GetService<ITransactionAnomalyDetector>();
        anomalyDetector.Should().NotBeNull();
        anomalyDetector.Should().BeOfType<AnomalyDetector>();

        var redisService = serviceProvider.GetService<IRedisCacheService>();
        redisService.Should().BeNull();
    }

    [Fact]
    public void AddAnomalyDetection_WithCaseSensitiveMode_ShouldHandleCorrectly()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "STATEFUL", // Uppercase
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0",
                ["Redis:ConnectionString"] = "localhost:6379"
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var anomalyDetector = serviceProvider.GetService<ITransactionAnomalyDetector>();
        anomalyDetector.Should().NotBeNull();
        anomalyDetector.Should().BeOfType<StatefulAnomalyDetector>();

        var redisService = serviceProvider.GetService<IRedisCacheService>();
        redisService.Should().NotBeNull();
    }

    [Fact]
    public void AddAnomalyDetection_WithInvalidMode_ShouldDefaultToStateless()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "invalid-mode",
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0"
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var anomalyDetector = serviceProvider.GetService<ITransactionAnomalyDetector>();
        anomalyDetector.Should().NotBeNull();
        anomalyDetector.Should().BeOfType<AnomalyDetector>();

        var redisService = serviceProvider.GetService<IRedisCacheService>();
        redisService.Should().BeNull();
    }

    [Fact]
    public void AddAnomalyDetection_ShouldRegisterAnomalyDetectionSettingsConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:MinimumTransactionCount"] = "3",
                ["AnomalyDetection:HighValueDeviationFactor"] = "15.0"
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<AnomalyDetectionSettings>>();
        options.Should().NotBeNull();
        options!.Value.MinimumTransactionCount.Should().Be(3);
        options!.Value.HighValueDeviationFactor.Should().Be(15.0);
    }

    [Fact]
    public void AddAnomalyDetection_WithStatefulMode_ShouldRegisterRedisSettingsConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateful",
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0",
                ["Redis:ConnectionString"] = "production.redis.cache.windows.net",
                ["Redis:Database"] = "1"
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var redisOptions = serviceProvider.GetService<IOptions<RedisSettings>>();
        redisOptions.Should().NotBeNull();
        redisOptions!.Value.ConnectionString.Should().Be("production.redis.cache.windows.net");
    }

    [Fact]
    public void AddAnomalyDetection_WithStatelessMode_ShouldNotRegisterRedisSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateless",
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0"
                // No Redis configuration
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Should not have Redis-related services
        var redisOptions = serviceProvider.GetService<IOptions<RedisSettings>>();
        var redisService = serviceProvider.GetService<IRedisCacheService>();

        redisOptions.Should().BeNull();
        redisService.Should().BeNull();
    }

    [Fact]
    public void AddAnomalyDetection_ShouldRegisterServicesWithCorrectLifetime()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateful",
                ["AnomalyDetection:MinimumTransactionCount"] = "5",
                ["AnomalyDetection:HighValueDeviationFactor"] = "10.0",
                ["Redis:ConnectionString"] = "localhost:6379"
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionAnomalyDetector));
        serviceDescriptor.Should().NotBeNull();
        serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var redisServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IRedisCacheService));
        redisServiceDescriptor.Should().NotBeNull();
        redisServiceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddAnomalyDetection_WithCompleteStatefulConfiguration_ShouldConfigureAllServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:Mode"] = "stateful",
                ["AnomalyDetection:MinimumTransactionCount"] = "10",
                ["AnomalyDetection:HighValueDeviationFactor"] = "8.0",
                ["Redis:ConnectionString"] = "production.redis.cache.windows.net",
                ["Redis:Database"] = "2"
            })
            .Build();

        services.AddAnomalyDetection(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var anomalyOptions = serviceProvider.GetRequiredService<IOptions<AnomalyDetectionSettings>>();
        anomalyOptions.Value.MinimumTransactionCount.Should().Be(10);
        anomalyOptions.Value.HighValueDeviationFactor.Should().Be(8.0);

        var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisSettings>>();
        redisOptions.Value.ConnectionString.Should().Be("production.redis.cache.windows.net");

        var anomalyDetector = serviceProvider.GetRequiredService<ITransactionAnomalyDetector>();
        anomalyDetector.Should().BeOfType<StatefulAnomalyDetector>();

        var redisService = serviceProvider.GetRequiredService<IRedisCacheService>();
        redisService.Should().BeOfType<RedisCacheService>();
    }
}
