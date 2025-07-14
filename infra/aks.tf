# infra/aks.tf
resource "azurerm_kubernetes_cluster" "aks" {
  count               = var.deployment_architecture == "aks" ? 1 : 0
  name                = "${var.resource_prefix}-aks-${random_id.suffix.hex}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  dns_prefix          = "${var.resource_prefix}-aks"
  sku_tier            = "Free"

  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  default_node_pool {
    name                = "default"
    node_count          = var.enable_cost_optimization ? 1 : 2
    vm_size             = "Standard_B2s"
    enable_auto_scaling = true
    min_count           = 1
    max_count           = var.development_mode ? 2 : 5
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
