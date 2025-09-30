output "account_id" {
  description = "The ID of the Cosmos DB account"
  value       = azurerm_cosmosdb_account.main.id
}

output "endpoint" {
  description = "The endpoint of the Cosmos DB account"
  value       = azurerm_cosmosdb_account.main.endpoint
}

output "primary_key" {
  description = "The primary key of the Cosmos DB account"
  value       = azurerm_cosmosdb_account.main.primary_key
  sensitive   = true
}

output "connection_strings" {
  description = "The connection strings of the Cosmos DB account"
  value       = azurerm_cosmosdb_account.main.connection_strings
  sensitive   = true
}

output "database_name" {
  description = "The name of the Cosmos DB database"
  value       = azurerm_cosmosdb_sql_database.main.name
}

output "resource_group_name" {
  description = "The name of the Cosmos DB resource group"
  value       = azurerm_resource_group.cosmos.name
}