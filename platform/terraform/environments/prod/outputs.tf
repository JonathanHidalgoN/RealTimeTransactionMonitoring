output "aks_cluster_id" {
  description = "The ID of the AKS cluster"
  value       = module.aks_cluster.cluster_id
}

output "aks_cluster_name" {
  description = "The name of the AKS cluster"
  value       = module.aks_cluster.cluster_name
}

output "key_vault_id" {
  description = "The ID of the Key Vault"
  value       = module.security.key_vault_id
}

output "cosmos_db_endpoint" {
  description = "The endpoint of the Cosmos DB account"
  value       = module.cosmos_db.endpoint
}

output "resource_group_name" {
  description = "The name of the main resource group"
  value       = module.aks_cluster.resource_group_name
}