output "static_web_app_id" {
  description = "Static Web App resource ID"
  value       = azurerm_static_web_app.main.id
}

output "default_hostname" {
  description = "Static Web App default hostname"
  value       = azurerm_static_web_app.main.default_host_name
}

output "deployment_token" {
  description = "Static Web App deployment token"
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
}

output "static_web_app_name" {
  description = "Static Web App name"
  value       = azurerm_static_web_app.main.name
}
