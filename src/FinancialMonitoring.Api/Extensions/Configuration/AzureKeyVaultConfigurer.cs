using Azure.Identity;
using FinancialMonitoring.Abstractions;

namespace FinancialMonitoring.Api.Extensions.Configuration;

/// <summary>
/// Production implementation of Key Vault configuration using Azure SDK
/// </summary>
public class AzureKeyVaultConfigurer : IKeyVaultConfigurer
{
    public void ConfigureKeyVault(IConfigurationBuilder configurationBuilder, Uri keyVaultUri)
    {
        configurationBuilder.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}
