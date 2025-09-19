using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using FinancialMonitoring.Api.Extensions.ServiceRegistration;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Models;
using Xunit;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class AuthenticationExtensionsTests
{
    [Fact]
    public void AddAuthenticationServices_ShouldRegisterJwtSettings()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-with-minimum-length-required",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ValidateIssuer"] = "true",
                ["Jwt:ValidateAudience"] = "true",
                ["Jwt:ValidateLifetime"] = "true",
                ["Jwt:ValidateIssuerSigningKey"] = "true",
                ["Jwt:ClockSkewMinutes"] = "5"
            })
            .Build();

        services.AddAuthenticationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var jwtOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<JwtSettings>>();

        jwtOptions.Should().NotBeNull();
        jwtOptions!.Value.SecretKey.Should().Be("test-secret-key-with-minimum-length-required");
        jwtOptions.Value.Issuer.Should().Be("test-issuer");
        jwtOptions.Value.Audience.Should().Be("test-audience");
    }

    [Fact]
    public void AddAuthenticationServices_ShouldRegisterAuthenticationSchemes()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-with-minimum-length-required",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        services.AddAuthenticationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var authSchemeProvider = serviceProvider.GetService<IAuthenticationSchemeProvider>();

        authSchemeProvider.Should().NotBeNull();

        var schemes = authSchemeProvider!.GetAllSchemesAsync().Result;
        schemes.Should().Contain(s => s.Name == SecureApiKeyAuthenticationDefaults.SchemeName);
        schemes.Should().Contain(s => s.Name == JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void AddAuthenticationServices_ShouldRegisterAuthorizationPolicies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-with-minimum-length-required",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        services.AddAuthenticationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();

        authorizationOptions.Should().NotBeNull();

        var options = authorizationOptions!.Value;
        options.GetPolicy(AppConstants.AdminRole).Should().NotBeNull();
        options.GetPolicy(AppConstants.ViewerRole).Should().NotBeNull();
        options.GetPolicy(AppConstants.AnalystRole).Should().NotBeNull();
    }

    [Fact]
    public void AddAuthenticationServices_WithMinimalConfig_ShouldUseDefaults()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        services.AddAuthenticationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var jwtOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<JwtSettings>>();

        jwtOptions.Should().NotBeNull();
        var jwtSettings = jwtOptions!.Value;
        jwtSettings.Should().NotBeNull();
    }

    [Fact]
    public void AddAuthenticationServices_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-with-minimum-length-required"
            })
            .Build();

        var result = services.AddAuthenticationServices(configuration);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAuthenticationServices_ShouldConfigureJwtBearerOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-with-minimum-length-required-for-jwt-signing",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ValidateIssuer"] = "true",
                ["Jwt:ValidateAudience"] = "true",
                ["Jwt:ClockSkewMinutes"] = "10"
            })
            .Build();

        services.AddAuthenticationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var jwtOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptionsMonitor<JwtBearerOptions>>();

        jwtOptions.Should().NotBeNull();
        var options = jwtOptions!.Get(JwtBearerDefaults.AuthenticationScheme);

        options.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        options.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        options.TokenValidationParameters.ValidIssuer.Should().Be("test-issuer");
        options.TokenValidationParameters.ValidAudience.Should().Be("test-audience");
        options.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromMinutes(10));
    }
}