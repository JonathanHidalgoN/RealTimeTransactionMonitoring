using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Extensions.Configuration;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions;
using Moq;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class EnvironmentDetectorTests
{
    [Fact]
    public void DetectAndConfigureEnvironment_Development_ShouldReturnDevelopment()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Development"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(builder);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Testing_ShouldReturnTesting()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Testing"
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(builder);

        result.Should().Be(RunTimeEnvironment.Testing);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_NoEnvironmentSet_ShouldDefaultToDevelopment()
    {
        var builder = WebApplication.CreateBuilder();

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(builder);

        result.Should().Be(RunTimeEnvironment.Development);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Production_WithValidKeyVault_ShouldConfigureKeyVault()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = "https://test-keyvault.vault.azure.net/"
        });

        var mockKeyVaultConfigurer = new Mock<IKeyVaultConfigurer>();

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(builder, mockKeyVaultConfigurer.Object);

        result.Should().Be(RunTimeEnvironment.Production);
        mockKeyVaultConfigurer.Verify(x => x.ConfigureKeyVault(
            It.IsAny<IConfigurationBuilder>(),
            It.Is<Uri>(uri => uri.ToString() == "https://test-keyvault.vault.azure.net/")),
            Times.Once);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Production_WithoutKeyVaultUri_ShouldThrowException()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production"
        });

        var mockKeyVaultConfigurer = new Mock<IKeyVaultConfigurer>();

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(builder, mockKeyVaultConfigurer.Object);

        action.Should().Throw<ArgumentException>();

        mockKeyVaultConfigurer.Verify(x => x.ConfigureKeyVault(It.IsAny<IConfigurationBuilder>(), It.IsAny<Uri>()), Times.Never);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Production_WithEmptyKeyVaultUri_ShouldThrowException()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = ""
        });

        var mockKeyVaultConfigurer = new Mock<IKeyVaultConfigurer>();

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(builder, mockKeyVaultConfigurer.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Production_WithInvalidKeyVaultUri_ShouldThrowException()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = "invalid-uri"
        });

        var mockKeyVaultConfigurer = new Mock<IKeyVaultConfigurer>();

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(builder, mockKeyVaultConfigurer.Object);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Production_KeyVaultThrowsException_ShouldPropagateException()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Production",
            ["KEY_VAULT_URI"] = "https://test-keyvault.vault.azure.net/"
        });

        var mockKeyVaultConfigurer = new Mock<IKeyVaultConfigurer>();
        mockKeyVaultConfigurer.Setup(x => x.ConfigureKeyVault(It.IsAny<IConfigurationBuilder>(), It.IsAny<Uri>()))
                             .Throws(new InvalidOperationException("Azure connection failed"));

        var action = () => EnvironmentDetector.DetectAndConfigureEnvironment(builder, mockKeyVaultConfigurer.Object);

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("development", RunTimeEnvironment.Development)]
    [InlineData("DEVELOPMENT", RunTimeEnvironment.Development)]
    [InlineData("testing", RunTimeEnvironment.Testing)]
    [InlineData("TESTING", RunTimeEnvironment.Testing)]
    public void DetectAndConfigureEnvironment_ShouldBeCaseInsensitive(string environmentString, RunTimeEnvironment expected)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = environmentString
        });

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(builder);

        result.Should().Be(expected);
    }

    [Fact]
    public void DetectAndConfigureEnvironment_Development_ShouldNotCallKeyVaultConfigurer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AppConstants.runTimeEnvVarName] = "Development"
        });

        var mockKeyVaultConfigurer = new Mock<IKeyVaultConfigurer>();

        var result = EnvironmentDetector.DetectAndConfigureEnvironment(builder, mockKeyVaultConfigurer.Object);

        result.Should().Be(RunTimeEnvironment.Development);
        mockKeyVaultConfigurer.Verify(x => x.ConfigureKeyVault(It.IsAny<IConfigurationBuilder>(), It.IsAny<Uri>()), Times.Never);
    }
}
