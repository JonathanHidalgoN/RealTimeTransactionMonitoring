output "acr_login_server" {
  description = "Container Registry login server URL"
  value       = azurerm_container_registry.main.login_server
}

output "acr_admin_username" {
  description = "Container Registry admin username"
  value       = azurerm_container_registry.main.admin_username
}

output "acr_admin_password" {
  description = "Container Registry admin password"
  value       = azurerm_container_registry.main.admin_password
  sensitive   = true
}

output "acr_name" {
  description = "Container Registry name"
  value       = azurerm_container_registry.main.name
}

output "resource_group_name" {
  description = "Shared resource group name"
  value       = azurerm_resource_group.shared.name
}
