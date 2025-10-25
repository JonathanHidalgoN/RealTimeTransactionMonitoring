output "api_url" {
  description = "API endpoint URL - Give this to recruiters!"
  value       = module.container_apps.api_url
}

output "frontend_url" {
  description = "Frontend application URL"
  value       = "https://${module.frontend.default_hostname}"
}

output "cosmos_endpoint" {
  description = "Cosmos DB endpoint"
  value       = module.data.cosmos_endpoint
}

output "cosmos_database_name" {
  description = "Cosmos DB database name"
  value       = module.data.cosmos_database_name
}

output "eventhub_namespace" {
  description = "Event Hubs namespace"
  value       = module.data.eventhub_namespace_name
}

output "app_insights_connection_string" {
  description = "Application Insights connection string"
  value       = module.monitoring.app_insights_connection_string
  sensitive   = true
}

output "acr_login_server" {
  description = "Container Registry login server"
  value       = data.terraform_remote_state.shared.outputs.acr_login_server
}

output "acr_name" {
  description = "Container Registry name"
  value       = data.terraform_remote_state.shared.outputs.acr_name
}

output "frontend_deployment_token" {
  description = "Static Web App deployment token"
  value       = module.frontend.deployment_token
  sensitive   = true
}

output "container_app_environment" {
  description = "Container Apps Environment name"
  value       = module.container_apps.container_app_environment_name
}

output "key_vault_uri" {
  description = "Key Vault URI"
  value       = module.security.key_vault_uri
}

output "static_web_app_name" {
  description = "Static Web App name"
  value       = module.frontend.static_web_app_name
}

output "resource_group_name" {
  description = "Resource Group name"
  value       = azurerm_resource_group.main.name
}
