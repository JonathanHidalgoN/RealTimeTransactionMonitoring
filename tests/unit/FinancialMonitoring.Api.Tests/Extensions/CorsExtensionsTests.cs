using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using FinancialMonitoring.Api.Extensions.ServiceRegistration;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class CorsExtensionsTests
{
    [Fact]
    public void AddCorsConfiguration_WithDefaultSettings_ShouldRegisterCorsPolicy()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();
        var portSettings = new PortSettings { BlazorHttp = 8081, BlazorHttps = 8082 };

        var result = services.AddCorsConfiguration(configuration, portSettings);

        result.Should().BeSameAs(services);

        var serviceProvider = services.BuildServiceProvider();
        var corsService = serviceProvider.GetService<ICorsService>();
        corsService.Should().NotBeNull();
    }

    [Fact]
    public void AddCorsConfiguration_WithCustomOrigins_ShouldUseConfiguredOrigins()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://custom1.com",
                ["Cors:AllowedOrigins:1"] = "https://custom2.com"
            })
            .Build();
        var portSettings = new PortSettings { BlazorHttp = 8081, BlazorHttps = 8082 };

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsService = serviceProvider.GetRequiredService<ICorsService>();
        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.Should().NotBeNull();
        corsPolicy.Origins.Should().Contain("https://custom1.com");
        corsPolicy.Origins.Should().Contain("https://custom2.com");
        corsPolicy.Origins.Should().NotContain("http://localhost:8081");
        corsPolicy.Origins.Should().NotContain("https://localhost:8082");
    }

    [Fact]
    public void AddCorsConfiguration_WithEmptyOrigins_ShouldUseDefaultOrigins()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();
        var portSettings = new PortSettings { BlazorHttp = 9000, BlazorHttps = 9443 };

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.Origins.Should().Contain("http://localhost:9000");
        corsPolicy.Origins.Should().Contain("https://localhost:9443");
    }

    [Fact]
    public void AddCorsConfiguration_WithCustomHeaders_ShouldUseConfiguredHeaders()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedHeaders:0"] = "Content-Type",
                ["Cors:AllowedHeaders:1"] = "Authorization"
            })
            .Build();
        var portSettings = new PortSettings();

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.Headers.Should().Contain("Content-Type");
        corsPolicy.Headers.Should().Contain("Authorization");
    }

    [Fact]
    public void AddCorsConfiguration_WithCustomMethods_ShouldUseConfiguredMethods()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedMethods:0"] = "GET",
                ["Cors:AllowedMethods:1"] = "POST",
                ["Cors:AllowedMethods:2"] = "PUT"
            })
            .Build();
        var portSettings = new PortSettings();

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.Methods.Should().Contain("GET");
        corsPolicy.Methods.Should().Contain("POST");
        corsPolicy.Methods.Should().Contain("PUT");
    }

    [Fact]
    public void AddCorsConfiguration_WithAllowCredentialsTrue_ShouldAllowCredentials()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowCredentials"] = "true"
            })
            .Build();
        var portSettings = new PortSettings();

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddCorsConfiguration_WithAllowCredentialsFalse_ShouldNotAllowCredentials()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowCredentials"] = "false"
            })
            .Build();
        var portSettings = new PortSettings();

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.SupportsCredentials.Should().BeFalse();
    }

    [Fact]
    public void AddCorsConfiguration_WithCompleteConfiguration_ShouldApplyAllSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.com",
                ["Cors:AllowedHeaders:0"] = "Content-Type",
                ["Cors:AllowedHeaders:1"] = "Authorization",
                ["Cors:AllowedMethods:0"] = "GET",
                ["Cors:AllowedMethods:1"] = "POST",
                ["Cors:AllowCredentials"] = "true"
            })
            .Build();
        var portSettings = new PortSettings { BlazorHttp = 8081, BlazorHttps = 8082 };

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsPolicy = GetCorsPolicy(serviceProvider);

        corsPolicy.Origins.Should().Contain("https://app.example.com");
        corsPolicy.Headers.Should().Contain("Content-Type");
        corsPolicy.Headers.Should().Contain("Authorization");
        corsPolicy.Methods.Should().Contain("GET");
        corsPolicy.Methods.Should().Contain("POST");
        corsPolicy.SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddCorsConfiguration_ShouldRegisterCorsSettingsInDI()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowCredentials"] = "false"
            })
            .Build();
        var portSettings = new PortSettings();

        services.AddCorsConfiguration(configuration, portSettings);
        var serviceProvider = services.BuildServiceProvider();

        var corsOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CorsSettings>>();
        corsOptions.Should().NotBeNull();
        corsOptions?.Value.AllowCredentials.Should().BeFalse();
    }

    private static CorsPolicy GetCorsPolicy(IServiceProvider serviceProvider)
    {
        var corsService = serviceProvider.GetRequiredService<ICorsService>();
        var policyProvider = serviceProvider.GetRequiredService<ICorsPolicyProvider>();

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;

        var corsPolicy = policyProvider.GetPolicyAsync(httpContext, "_myAllowSpecificOrigins").Result;
        corsPolicy.Should().NotBeNull();

        return corsPolicy!;
    }
}