output "container_app_environment_id" {
  description = "Container Apps Environment ID"
  value       = azurerm_container_app_environment.main.id
}

output "container_app_environment_name" {
  description = "Container Apps Environment name"
  value       = azurerm_container_app_environment.main.name
}

output "api_fqdn" {
  description = "API fully qualified domain name"
  value       = azurerm_container_app.api.ingress[0].fqdn
}

output "api_url" {
  description = "API URL"
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "processor_name" {
  description = "Transaction Processor container app name"
  value       = azurerm_container_app.processor.name
}

output "simulator_name" {
  description = "Transaction Simulator container app name"
  value       = azurerm_container_app.simulator.name
}

output "api_identity_principal_id" {
  description = "API managed identity principal ID"
  value       = azurerm_container_app.api.identity[0].principal_id
}

output "processor_identity_principal_id" {
  description = "Processor managed identity principal ID"
  value       = azurerm_container_app.processor.identity[0].principal_id
}

output "simulator_identity_principal_id" {
  description = "Simulator managed identity principal ID"
  value       = azurerm_container_app.simulator.identity[0].principal_id
}
