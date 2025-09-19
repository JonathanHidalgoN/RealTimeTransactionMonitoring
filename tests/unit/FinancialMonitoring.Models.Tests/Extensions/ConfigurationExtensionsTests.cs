using FluentAssertions;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Extensions;
using Xunit;

namespace FinancialMonitoring.Models.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void BuildPortSettings_WithValidPorts_ShouldUseParsedValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = "8080",
                ["BLAZOR_HTTP_PORT"] = "8081",
                ["BLAZOR_HTTPS_PORT"] = "8082",
                ["MONGODB_PORT"] = "27017"
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(8080);
        result.BlazorHttp.Should().Be(8081);
        result.BlazorHttps.Should().Be(8082);
        result.MongoDb.Should().Be(27017);
    }

    [Fact]
    public void BuildPortSettings_WithMissingPorts_ShouldUseDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(AppConstants.DefaultApiPort);
        result.BlazorHttp.Should().Be(AppConstants.DefaultBlazorHttpPort);
        result.BlazorHttps.Should().Be(AppConstants.DefaultBlazorHttpsPort);
        result.MongoDb.Should().Be(AppConstants.DefaultMongoDbPort);
    }

    [Fact]
    public void BuildPortSettings_WithInvalidPorts_ShouldUseDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = "not-a-number",
                ["BLAZOR_HTTP_PORT"] = "invalid",
                ["BLAZOR_HTTPS_PORT"] = "",
                ["MONGODB_PORT"] = "abc123"
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(AppConstants.DefaultApiPort);
        result.BlazorHttp.Should().Be(AppConstants.DefaultBlazorHttpPort);
        result.BlazorHttps.Should().Be(AppConstants.DefaultBlazorHttpsPort);
        result.MongoDb.Should().Be(AppConstants.DefaultMongoDbPort);
    }

    [Fact]
    public void BuildPortSettings_WithMixedValidInvalid_ShouldMixDefaultsAndParsed()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = "9000",           // Valid
                ["BLAZOR_HTTP_PORT"] = "invalid", // Invalid
                ["BLAZOR_HTTPS_PORT"] = "9443",   // Valid
                ["MONGODB_PORT"] = ""             // Invalid (empty)
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(9000);
        result.BlazorHttp.Should().Be(AppConstants.DefaultBlazorHttpPort);
        result.BlazorHttps.Should().Be(9443);
        result.MongoDb.Should().Be(AppConstants.DefaultMongoDbPort);
    }

    [Theory]
    [InlineData("8080", 8080)]
    [InlineData("5000", 5000)]
    [InlineData("65535", 65535)]
    [InlineData("1024", 1024)]
    public void BuildPortSettings_ApiPort_WithValidNumbers_ShouldUseParsedValue(string portString, int expectedPort)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = portString
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(expectedPort);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("port8080")]
    [InlineData("8080port")]
    [InlineData("80.80")]
    public void BuildPortSettings_ApiPort_WithInvalidValues_ShouldUseDefault(string invalidPortString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = invalidPortString
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(AppConstants.DefaultApiPort);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("99999")]
    [InlineData("0")]
    [InlineData("1023")]
    [InlineData("65536")]
    public void BuildPortSettings_ApiPort_WithOutOfRangeValues_ShouldThrowException(string outOfRangePortString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = outOfRangePortString
            })
            .Build();

        var action = () => configuration.BuildPortSettings();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Port configuration validation failed:*");
    }

    [Fact]
    public void BuildPortSettings_WithNullValues_ShouldUseDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = null,
                ["BLAZOR_HTTP_PORT"] = null,
                ["BLAZOR_HTTPS_PORT"] = null,
                ["MONGODB_PORT"] = null
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(AppConstants.DefaultApiPort);
        result.BlazorHttp.Should().Be(AppConstants.DefaultBlazorHttpPort);
        result.BlazorHttps.Should().Be(AppConstants.DefaultBlazorHttpsPort);
        result.MongoDb.Should().Be(AppConstants.DefaultMongoDbPort);
    }

    [Fact]
    public void BuildPortSettings_ShouldReturnNewInstanceEachTime()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = "8080"
            })
            .Build();

        var result1 = configuration.BuildPortSettings();
        var result2 = configuration.BuildPortSettings();

        result1.Should().NotBeSameAs(result2);
        result1.Api.Should().Be(result2.Api);
    }

    [Fact]
    public void BuildPortSettings_WithValidEdgeCasePortNumbers_ShouldUseParsedValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = "1024"  // Minimum valid port
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(1024);
    }

    [Fact]
    public void BuildPortSettings_WithMaxValidPort_ShouldUseParsedValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_PORT"] = "65535"  // Maximum valid port
            })
            .Build();

        var result = configuration.BuildPortSettings();

        result.Api.Should().Be(65535);
    }
}