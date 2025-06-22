resource "azurerm_user_assigned_identity" "app_identity" {
  name                = "${var.resource_prefix}-app-identity"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_federated_identity_credential" "app_identity_federation" {
  name                = "${var.resource_prefix}-app-identity-federation"
  resource_group_name = azurerm_resource_group.rg.name
  parent_id           = azurerm_user_assigned_identity.app_identity.id

  audience = ["api://AzureADTokenExchange"]
  issuer   = azurerm_kubernetes_cluster.aks.oidc_issuer_url
  subject  = "system:serviceaccount:finmon-app:finmon-app-sa"
}
