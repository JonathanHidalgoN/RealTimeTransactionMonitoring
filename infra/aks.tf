# infra/aks.tf
resource "azurerm_kubernetes_cluster" "aks" {
  name                = "${var.resource_prefix}-aks"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  dns_prefix          = "${var.resource_prefix}-aks"
  sku_tier            = "Free"

  oidc_issuer_profile {
    enabled = true
  }

  workload_identity_profile {
    enabled = true
  }

  default_node_pool {
    name                = "default"
    node_count          = 1
    vm_size             = "Standard_B1s"
    enable_auto_scaling = true
    min_count           = 1
    max_count           = 2
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
