resource "azurerm_kubernetes_cluster" "aks" {
  name                = "${var.resource_prefix}-aks"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  dns_prefix          = "${var.resource_prefix}-aks"
  sku_tier            = "Free"

  default_node_pool {
    name       = "default"
    node_count = 1
    vm_size    = "Standard_B2s"

    enable_auto_scaling = true
    #Could use 0 to save a lot on idle times but now using free credits
    # min_count           = 1
    min_count = 1
    max_count = 2
  }

  identity {
    type = "SystemAssigned"
  }

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
