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

output "eventhub_checkpoint_storage_connection_string" {
  description = "The connection string for the Event Hubs checkpoint storage account."
  value       = azurerm_storage_account.eh_checkpoints.primary_connection_string
  sensitive   = true
}

output "eventhubs_namespace_connection_string" {
  description = "The primary connection string for the Event Hubs Namespace."
  value       = azurerm_eventhub_namespace.eh_namespace.default_primary_connection_string
  sensitive   = true
}

output "eventhub_name" {
  description = "The name of the transactions Event Hub."
  value       = azurerm_eventhub.transactions.name
}

output "eventhub_checkpoint_container_name" {
  description = "The name of the blob container for Event Hubs checkpoints."
  value       = azurerm_storage_container.eh_checkpoints.name
}

output "anomalies_eventhub_name" {
  description = "The name of the anomalies Event Hub."
  value       = azurerm_eventhub.anomalies.name
}

output "logic_app_name" {
  description = "The name of the notification Logic App."
  value       = azurerm_logic_app_workflow.notification_workflow.name
}

output "redis_connection_string" {
  description = "The primary connection string for the Azure Cache for Redis instance."
  value       = azurerm_redis_cache.cache.primary_connection_string
  sensitive   = true
}

output "app_managed_identity_client_id" {
  description = "The Client ID of the application's User-Assigned Managed Identity."
  value       = azurerm_user_assigned_identity.app_identity.client_id
}
