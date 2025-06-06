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
