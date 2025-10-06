output "cosmos_endpoint" {
  description = "Cosmos DB endpoint"
  value       = azurerm_cosmosdb_account.main.endpoint
}

output "cosmos_connection_string" {
  description = "Cosmos DB connection string"
  value       = azurerm_cosmosdb_account.main.primary_sql_connection_string
  sensitive   = true
}

output "cosmos_primary_key" {
  description = "Cosmos DB primary key"
  value       = azurerm_cosmosdb_account.main.primary_key
  sensitive   = true
}

output "cosmos_database_name" {
  description = "Cosmos DB database name"
  value       = azurerm_cosmosdb_sql_database.main.name
}

output "cosmos_container_name" {
  description = "Cosmos DB container name"
  value       = azurerm_cosmosdb_sql_container.transactions.name
}

output "cosmos_partition_key_path" {
  description = "Cosmos DB partition key path"
  value       = "/id"
}

output "eventhub_namespace_name" {
  description = "Event Hubs namespace name"
  value       = azurerm_eventhub_namespace.main.name
}

output "eventhub_connection_string" {
  description = "Event Hubs connection string"
  value       = azurerm_eventhub_namespace.main.default_primary_connection_string
  sensitive   = true
}

output "eventhub_transactions_name" {
  description = "Transactions Event Hub name"
  value       = azurerm_eventhub.transactions.name
}

output "eventhub_anomalies_name" {
  description = "Anomalies Event Hub name"
  value       = azurerm_eventhub.anomalies.name
}

output "storage_account_name" {
  description = "Storage account name for checkpoints"
  value       = azurerm_storage_account.eventhub_checkpoints.name
}

output "checkpoint_container_name" {
  description = "Checkpoint container name"
  value       = azurerm_storage_container.checkpoints.name
}

output "storage_connection_string" {
  description = "Storage account connection string"
  value       = azurerm_storage_account.eventhub_checkpoints.primary_connection_string
  sensitive   = true
}
