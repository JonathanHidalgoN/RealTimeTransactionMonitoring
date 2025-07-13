resource "azurerm_role_assignment" "app_identity_kv_secrets_user" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}

resource "azurerm_role_assignment" "admin_user_kv_admin" {
  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = var.admin_user_object_id
}

resource "azurerm_role_assignment" "aks_acr_pull" {
  count                = var.deployment_architecture == "aks" ? 1 : 0
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.aks[0].kubelet_identity[0].object_id
}

# Container Apps need ACR pull access via managed identity
resource "azurerm_role_assignment" "app_identity_acr_pull" {
  count                = var.deployment_architecture == "containerapp" ? 1 : 0
  scope                = azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.app_identity.principal_id
}

