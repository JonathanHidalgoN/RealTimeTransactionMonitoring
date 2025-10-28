output "key_vault_id" {
  description = "Key Vault resource ID"
  value       = azurerm_key_vault.main.id
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = azurerm_key_vault.main.vault_uri
}

output "key_vault_name" {
  description = "Key Vault name"
  value       = azurerm_key_vault.main.name
}

output "managed_identity_id" {
  description = "Managed Identity resource ID"
  value       = azurerm_user_assigned_identity.container_apps.id
}

output "managed_identity_client_id" {
  description = "Managed Identity client ID"
  value       = azurerm_user_assigned_identity.container_apps.client_id
}

output "managed_identity_principal_id" {
  description = "Managed Identity principal ID"
  value       = azurerm_user_assigned_identity.container_apps.principal_id
}
