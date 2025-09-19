using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.OutputCaching;
using FinancialMonitoring.Api.Extensions.ServiceRegistration;
using FinancialMonitoring.Models;
using Xunit;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class CachingExtensionsTests
{
    [Fact]
    public void AddCaching_ShouldRegisterCacheSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:BasePolicyCacheSeconds"] = "300",
                ["Cache:TransactionCacheSeconds"] = "60",
                ["Cache:TransactionByIdCacheSeconds"] = "120",
                ["Cache:AnomalousTransactionCacheSeconds"] = "180",
                ["Cache:AnalyticsCacheSeconds"] = "600",
                ["Cache:TimeSeriesCacheSeconds"] = "300",
                ["ResponseCache:MaximumBodySizeBytes"] = "1048576",
                ["ResponseCache:SizeLimitBytes"] = "104857600",
                ["ResponseCache:UseCaseSensitivePaths"] = "true"
            })
            .Build();

        services.AddCaching(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var cacheOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CacheSettings>>();
        var responseCacheOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ResponseCacheSettings>>();

        cacheOptions.Should().NotBeNull();
        responseCacheOptions.Should().NotBeNull();

        cacheOptions!.Value.BasePolicyCacheSeconds.Should().Be(300);
        cacheOptions.Value.TransactionCacheSeconds.Should().Be(60);
        responseCacheOptions!.Value.MaximumBodySizeBytes.Should().Be(1048576);
    }

    [Fact]
    public void AddCaching_ShouldRegisterResponseCaching()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ResponseCache:MaximumBodySizeBytes"] = "2097152",
                ["ResponseCache:SizeLimitBytes"] = "209715200",
                ["ResponseCache:UseCaseSensitivePaths"] = "false"
            })
            .Build();

        services.AddCaching(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var responseCacheOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ResponseCacheSettings>>();

        responseCacheOptions.Should().NotBeNull();
        responseCacheOptions!.Value.MaximumBodySizeBytes.Should().Be(2097152);
    }

    [Fact]
    public void AddCaching_ShouldRegisterOutputCache()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:BasePolicyCacheSeconds"] = "300",
                ["Cache:TransactionCacheSeconds"] = "60"
            })
            .Build();

        services.AddCaching(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var outputCacheService = serviceProvider.GetService<IOutputCacheStore>();

        outputCacheService.Should().NotBeNull();
    }

    [Fact]
    public void AddCaching_ShouldConfigureOutputCachePolicies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:BasePolicyCacheSeconds"] = "300",
                ["Cache:TransactionCacheSeconds"] = "60",
                ["Cache:TransactionByIdCacheSeconds"] = "120",
                ["Cache:AnomalousTransactionCacheSeconds"] = "180",
                ["Cache:AnalyticsCacheSeconds"] = "600",
                ["Cache:TimeSeriesCacheSeconds"] = "300"
            })
            .Build();

        services.AddCaching(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var outputCacheOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<OutputCacheOptions>>();

        outputCacheOptions.Should().NotBeNull();
    }

    [Fact]
    public void AddCaching_WithDefaultConfig_ShouldUseDefaults()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        services.AddCaching(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var cacheOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CacheSettings>>();
        var responseCacheOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ResponseCacheSettings>>();

        cacheOptions.Should().NotBeNull();
        responseCacheOptions.Should().NotBeNull();

        var cacheSettings = cacheOptions!.Value;
        var responseCacheSettings = responseCacheOptions!.Value;

        cacheSettings.Should().NotBeNull();
        responseCacheSettings.Should().NotBeNull();
    }

    [Fact]
    public void AddCaching_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:BasePolicyCacheSeconds"] = "300"
            })
            .Build();

        var result = services.AddCaching(configuration);

        result.Should().BeSameAs(services);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("60")]
    [InlineData("300")]
    public void AddCaching_ShouldHandleDifferentCacheSeconds(string cacheSeconds)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:TransactionCacheSeconds"] = cacheSeconds,
                ["Cache:TransactionByIdCacheSeconds"] = cacheSeconds,
                ["Cache:AnomalousTransactionCacheSeconds"] = cacheSeconds,
                ["Cache:AnalyticsCacheSeconds"] = cacheSeconds,
                ["Cache:TimeSeriesCacheSeconds"] = cacheSeconds
            })
            .Build();

        var action = () => services.AddCaching(configuration);

        action.Should().NotThrow();
    }
}