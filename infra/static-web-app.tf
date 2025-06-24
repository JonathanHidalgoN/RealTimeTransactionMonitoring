resource "azurerm_static_web_app" "ui" {
  name                = "${var.resource_prefix}-webapp"
  location            = "centralus"
  resource_group_name = azurerm_resource_group.rg.name

  sku_tier = "Free"
  sku_size = "Free"

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
