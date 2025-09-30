resource "azurerm_resource_group" "aks" {
  name     = "${var.resource_prefix}-aks-${var.environment}-rg"
  location = var.azure_location

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_kubernetes_cluster" "main" {
  name                = "${var.resource_prefix}-aks-${var.environment}"
  location            = azurerm_resource_group.aks.location
  resource_group_name = azurerm_resource_group.aks.name
  dns_prefix          = "${var.resource_prefix}-aks-${var.environment}"
  sku_tier            = var.development_mode ? "Free" : "Standard"

  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  default_node_pool {
    name                = "default"
    node_count          = var.development_mode ? 1 : 3
    vm_size             = var.enable_cost_optimization ? "Standard_B2s" : "Standard_D2s_v3"
    enable_auto_scaling = true
    min_count           = var.development_mode ? 1 : 2
    max_count           = var.development_mode ? 2 : 10
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azuread_application" "workload_identity" {
  display_name = "${var.resource_prefix}-workload-identity-${var.environment}"
}

resource "azuread_service_principal" "workload_identity" {
  application_id = azuread_application.workload_identity.application_id
}

resource "azuread_application_federated_identity_credential" "workload_identity" {
  application_object_id = azuread_application.workload_identity.object_id
  display_name          = "${var.resource_prefix}-federated-${var.environment}"
  description           = "Federated identity for External Secrets Operator"
  audiences             = ["api://AzureADTokenExchange"]
  issuer                = azurerm_kubernetes_cluster.main.oidc_issuer_url
  subject               = "system:serviceaccount:external-secrets:external-secrets"
}

resource "azurerm_role_assignment" "workload_identity_key_vault" {
  scope                = var.key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azuread_service_principal.workload_identity.object_id

  depends_on = [azuread_service_principal.workload_identity]
}