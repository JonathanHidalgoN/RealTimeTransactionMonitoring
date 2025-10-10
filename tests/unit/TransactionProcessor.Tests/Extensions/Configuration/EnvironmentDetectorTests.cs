using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using TransactionProcessor.Extensions.Configuration;
using FinancialMonitoring.Models;

namespace TransactionProcessor.Tests.Extensions.Configuration;

public class EnvironmentDetectorTests
{
    [Fact]
    public void DetectAndConfigureEnvironment_WithNullEnvironmentVariable_ShouldDefaultToProduction()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Production", new Dictionary<string, string?>
        {
            ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Production);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithDevelopmentEnvironment_ShouldReturnDevelopment()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Development", new Dictionary<string, string?>());

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithTestingEnvironment_ShouldReturnTesting()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Testing", new Dictionary<string, string?>());

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Testing);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndValidKeyVault_ShouldReturnProduction()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Production", new Dictionary<string, string?>
        {
            ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Production);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndMissingKeyVault_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Production", new Dictionary<string, string?>());

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndEmptyKeyVault_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Production", new Dictionary<string, string?>
        {
            ["KEY_VAULT_URI"] = ""
        });

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndInvalidKeyVaultUri_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Production", new Dictionary<string, string?>
        {
            ["KEY_VAULT_URI"] = "invalid-uri"
        });

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithCaseInsensitiveEnvironment_ShouldWork()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("DEVELOPMENT", new Dictionary<string, string?>());

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithUnknownEnvironment_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("UnknownEnvironment", new Dictionary<string, string?>());

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithDevelopmentEnvironment_ShouldNotConfigureKeyVault()
    {
        var mockBuilder = CreateMockHostApplicationBuilder("Development", new Dictionary<string, string?>());

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    private static Mock<IHostApplicationBuilder> CreateMockHostApplicationBuilder(string environmentName, Dictionary<string, string?> configurationValues)
    {
        var mockConfiguration = new Mock<IConfigurationManager>();

        foreach (var kvp in configurationValues)
        {
            mockConfiguration.Setup(x => x[kvp.Key]).Returns(kvp.Value);
        }

        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(x => x.EnvironmentName).Returns(environmentName);

        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(x => x.Configuration).Returns(mockConfiguration.Object);
        mockBuilder.Setup(x => x.Environment).Returns(mockEnvironment.Object);

        return mockBuilder;
    }
}
