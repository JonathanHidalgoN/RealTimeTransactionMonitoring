using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using AspNetCoreRateLimit;
using FinancialMonitoring.Api.Extensions.ServiceRegistration;
using FinancialMonitoring.Models;
using Xunit;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class RateLimitingExtensionsTests
{
    [Fact]
    public void AddRateLimiting_ShouldRegisterRateLimitSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:EnableEndpointRateLimiting"] = "true",
                ["RateLimit:StackBlockedRequests"] = "false",
                ["RateLimit:HttpStatusCode"] = "429",
                ["RateLimit:RealIpHeader"] = "X-Real-IP",
                ["RateLimit:ClientIdHeader"] = "X-ClientId"
            })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var rateLimitOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<RateLimitSettings>>();

        rateLimitOptions.Should().NotBeNull();
        rateLimitOptions!.Value.EnableEndpointRateLimiting.Should().BeTrue();
        rateLimitOptions.Value.StackBlockedRequests.Should().BeFalse();
        rateLimitOptions.Value.HttpStatusCode.Should().Be(429);
        rateLimitOptions.Value.RealIpHeader.Should().Be("X-Real-IP");
        rateLimitOptions.Value.ClientIdHeader.Should().Be("X-ClientId");
    }

    [Fact]
    public void AddRateLimiting_ShouldRegisterMemoryCache()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var memoryCache = serviceProvider.GetService<IMemoryCache>();

        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void AddRateLimiting_ShouldRegisterInMemoryRateLimiting()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var rateLimitConfiguration = serviceProvider.GetService<IRateLimitConfiguration>();

        rateLimitConfiguration.Should().NotBeNull();
    }

    [Fact]
    public void AddRateLimiting_ShouldConfigureIpRateLimitOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:EnableEndpointRateLimiting"] = "true",
                ["RateLimit:StackBlockedRequests"] = "true",
                ["RateLimit:HttpStatusCode"] = "503",
                ["RateLimit:RealIpHeader"] = "X-Forwarded-For",
                ["RateLimit:ClientIdHeader"] = "X-Client-ID",
                ["RateLimit:GeneralRules:0:Endpoint"] = "*",
                ["RateLimit:GeneralRules:0:Period"] = "1m",
                ["RateLimit:GeneralRules:0:Limit"] = "100",
                ["RateLimit:GeneralRules:1:Endpoint"] = "POST:/api/v2/auth/token",
                ["RateLimit:GeneralRules:1:Period"] = "1h",
                ["RateLimit:GeneralRules:1:Limit"] = "50"
            })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var ipRateLimitOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<IpRateLimitOptions>>();

        ipRateLimitOptions.Should().NotBeNull();
        var options = ipRateLimitOptions!.Value;

        options.EnableEndpointRateLimiting.Should().BeTrue();
        options.StackBlockedRequests.Should().BeTrue();
        options.HttpStatusCode.Should().Be(503);
        options.RealIpHeader.Should().Be("X-Forwarded-For");
        options.ClientIdHeader.Should().Be("X-Client-ID");

        options.GeneralRules.Should().HaveCount(2);
        options.GeneralRules[0].Endpoint.Should().Be("*");
        options.GeneralRules[0].Period.Should().Be("1m");
        options.GeneralRules[0].Limit.Should().Be(100);
        options.GeneralRules[1].Endpoint.Should().Be("POST:/api/v2/auth/token");
        options.GeneralRules[1].Period.Should().Be("1h");
        options.GeneralRules[1].Limit.Should().Be(50);
    }

    [Fact]
    public void AddRateLimiting_WithDefaultConfig_ShouldUseDefaults()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var rateLimitOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<RateLimitSettings>>();

        rateLimitOptions.Should().NotBeNull();
        var rateLimitSettings = rateLimitOptions!.Value;
        rateLimitSettings.Should().NotBeNull();
    }

    [Fact]
    public void AddRateLimiting_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:EnableEndpointRateLimiting"] = "true"
            })
            .Build();

        var result = services.AddRateLimiting(configuration);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddRateLimiting_WithMultipleRules_ShouldMapAllRules()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:GeneralRules:0:Endpoint"] = "*",
                ["RateLimit:GeneralRules:0:Period"] = "1m",
                ["RateLimit:GeneralRules:0:Limit"] = "100",
                ["RateLimit:GeneralRules:1:Endpoint"] = "POST:/api/v2/auth/token",
                ["RateLimit:GeneralRules:1:Period"] = "1h",
                ["RateLimit:GeneralRules:1:Limit"] = "50",
                ["RateLimit:GeneralRules:2:Endpoint"] = "GET:/api/v2/transactions",
                ["RateLimit:GeneralRules:2:Period"] = "1s",
                ["RateLimit:GeneralRules:2:Limit"] = "10"
            })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var ipRateLimitOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<IpRateLimitOptions>>();

        ipRateLimitOptions.Should().NotBeNull();
        var options = ipRateLimitOptions!.Value;

        options.GeneralRules.Should().HaveCount(3);
        options.GeneralRules[2].Endpoint.Should().Be("GET:/api/v2/transactions");
        options.GeneralRules[2].Period.Should().Be("1s");
        options.GeneralRules[2].Limit.Should().Be(10);
    }

    [Fact]
    public void AddRateLimiting_WithNoRules_ShouldHaveEmptyRulesList()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:EnableEndpointRateLimiting"] = "true"
            })
            .Build();

        services.AddRateLimiting(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var ipRateLimitOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<IpRateLimitOptions>>();

        ipRateLimitOptions.Should().NotBeNull();
        var options = ipRateLimitOptions!.Value;

        options.GeneralRules.Should().BeEmpty();
    }
}
