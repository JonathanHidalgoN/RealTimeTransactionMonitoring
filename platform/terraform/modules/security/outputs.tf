output "key_vault_id" {
  description = "The ID of the Key Vault"
  value       = azurerm_key_vault.main.id
}

output "key_vault_name" {
  description = "The name of the Key Vault"
  value       = azurerm_key_vault.main.name
}

output "key_vault_uri" {
  description = "The URI of the Key Vault"
  value       = azurerm_key_vault.main.vault_uri
}

output "resource_group_name" {
  description = "The name of the security resource group"
  value       = azurerm_resource_group.security.name
}

output "resource_group_id" {
  description = "The ID of the security resource group"
  value       = azurerm_resource_group.security.id
}