output "application_insights_connection_string" {
  description = "The Connection String for Application Insights."
  value       = azurerm_application_insights.appi.connection_string
  sensitive   = true
}

output "key_vault_uri" {
  description = "The URI of the Azure Key Vault."
  value       = azurerm_key_vault.kv.vault_uri
  sensitive   = true
}

output "key_vault_name" {
  description = "The name of the Azure Key Vault."
  value       = azurerm_key_vault.kv.name
}

output "key_vault_id" {
  description = "The full resource ID of the Azure Key Vault."
  value       = azurerm_key_vault.kv.id
}

output "acr_login_server" {
  description = "The login server for the Azure Container Registry."
  value       = azurerm_container_registry.acr.login_server
}

output "acr_name" {
  description = "The name of the Azure Container Registry."
  value       = azurerm_container_registry.acr.name
}

output "aks_cluster_name" {
  description = "The name of the AKS cluster."
  value       = azurerm_kubernetes_cluster.aks.name
}

output "aks_cluster_id" {
  description = "The ID of the AKS cluster."
  value       = azurerm_kubernetes_cluster.aks.id
}

output "cosmosdb_endpoint" {
  description = "The endpoint URI of the Cosmos DB account."
  value       = azurerm_cosmosdb_account.db.endpoint
}

output "cosmosdb_primary_key" {
  description = "The primary key of the Cosmos DB account."
  value       = azurerm_cosmosdb_account.db.primary_key
  sensitive   = true
}

