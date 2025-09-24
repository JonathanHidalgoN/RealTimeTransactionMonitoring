using Microsoft.Extensions.Configuration;

namespace FinancialMonitoring.Abstractions;

/// <summary>
/// Abstracts Azure Key Vault configuration for dependency injection and testing
/// </summary>
public interface IKeyVaultConfigurer
{
    /// <summary>
    /// Configures Key Vault for the given configuration builder
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to extend</param>
    /// <param name="keyVaultUri">The Key Vault URI</param>
    void ConfigureKeyVault(IConfigurationBuilder configurationBuilder, Uri keyVaultUri);
}
