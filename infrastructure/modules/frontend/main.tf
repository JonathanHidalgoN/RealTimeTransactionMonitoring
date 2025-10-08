resource "azurerm_static_web_app" "main" {
  name                = "${var.resource_prefix}-webapp-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_tier            = "Free"
  sku_size            = "Free"

  tags = var.tags
}
