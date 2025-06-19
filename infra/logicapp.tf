resource "azurerm_logic_app_workflow" "notification_workflow" {
  name                = "${var.resource_prefix}-notification-logic-app"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
