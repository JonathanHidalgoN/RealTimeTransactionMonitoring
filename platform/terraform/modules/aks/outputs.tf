output "cluster_id" {
  description = "The ID of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.id
}

output "cluster_name" {
  description = "The name of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.name
}

output "resource_group_name" {
  description = "The name of the AKS resource group"
  value       = azurerm_resource_group.aks.name
}

output "resource_group_id" {
  description = "The ID of the AKS resource group"
  value       = azurerm_resource_group.aks.id
}

output "oidc_issuer_url" {
  description = "The OIDC issuer URL for workload identity"
  value       = azurerm_kubernetes_cluster.main.oidc_issuer_url
}

output "workload_identity_client_id" {
  description = "The client ID of the workload identity application"
  value       = azuread_application.workload_identity.application_id
}