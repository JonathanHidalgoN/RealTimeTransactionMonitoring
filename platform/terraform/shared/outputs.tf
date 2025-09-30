output "container_registry_name" {
  description = "The name of the Azure Container Registry"
  value       = azurerm_container_registry.main.name
}

output "container_registry_login_server" {
  description = "The login server of the Azure Container Registry"
  value       = azurerm_container_registry.main.login_server
}

output "tfstate_storage_account_name" {
  description = "The name of the Terraform state storage account"
  value       = azurerm_storage_account.tfstate.name
}

output "shared_resource_group_name" {
  description = "The name of the shared resource group"
  value       = azurerm_resource_group.shared.name
}