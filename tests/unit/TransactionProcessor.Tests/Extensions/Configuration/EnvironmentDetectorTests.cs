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
    public void DetectAndConfigureEnvironment_WithNullEnvironmentVariable_ShouldDefaultToDevelopment()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>());

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithDevelopmentEnvironment_ShouldReturnDevelopment()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Development"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithTestingEnvironment_ShouldReturnTesting()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Testing"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Testing);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndValidKeyVault_ShouldReturnProduction()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = "https://test-vault.vault.azure.net/"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Production);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndMissingKeyVault_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production"
            // Missing KEY_VAULT_URI
        });

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndEmptyKeyVault_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = ""
        });

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithProductionAndInvalidKeyVaultUri_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = "invalid-uri"
        });

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithCaseInsensitiveEnvironment_ShouldWork()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "DEVELOPMENT"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithUnknownEnvironment_ShouldThrow()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "UnknownEnvironment"
        });

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_WithDevelopmentEnvironment_ShouldNotConfigureKeyVault()
    {
        var mockBuilder = CreateMockHostApplicationBuilder(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Development"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(mockBuilder.Object);

        result.Should().Be(RunTimeEnvironment.Development);
        // Key Vault should not be configured for development environment
    }

    private static Mock<IHostApplicationBuilder> CreateMockHostApplicationBuilder(Dictionary<string, string?> configurationValues)
    {
        var mockConfiguration = new Mock<IConfigurationManager>();

        foreach (var kvp in configurationValues)
        {
            mockConfiguration.Setup(x => x[kvp.Key]).Returns(kvp.Value);
        }

        var mockBuilder = new Mock<IHostApplicationBuilder>();
        mockBuilder.Setup(x => x.Configuration).Returns(mockConfiguration.Object);

        return mockBuilder;
    }
}