using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

public class CosmosDbSettings
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Cosmos DB EndpointUri is required. Check Key Vault secret 'CosmosDb--EndpointUri' or environment variable 'CosmosDb__EndpointUri'.")]
    public string EndpointUri { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Cosmos DB PrimaryKey is required. Check Key Vault secret 'CosmosDb--PrimaryKey' or environment variable 'CosmosDb__PrimaryKey'.")]
    public string PrimaryKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Cosmos DB DatabaseName is required. Check Key Vault secret 'CosmosDb--DatabaseName' or environment variable 'CosmosDb__DatabaseName'.")]
    public string DatabaseName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Cosmos DB ContainerName is required. Check Key Vault secret 'CosmosDb--ContainerName' or environment variable 'CosmosDb__ContainerName'.")]
    public string ContainerName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Cosmos DB PartitionKeyPath is required. Check Key Vault secret 'CosmosDb--PartitionKeyPath' or environment variable 'CosmosDb__PartitionKeyPath'.")]
    public string PartitionKeyPath { get; set; } = string.Empty;
}