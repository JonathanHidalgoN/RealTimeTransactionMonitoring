output "static_web_app_id" {
  description = "Static Web App resource ID"
  value       = azurerm_static_web_app.main.id
}

output "default_hostname" {
  description = "Static Web App default hostname"
  value       = azurerm_static_web_app.main.default_host_name
}

output "api_key" {
  description = "Static Web App API key for deployment"
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
}
