resource "azurerm_resource_group" "rg" {
  name     = "${var.resource_prefix}-rg"
  location = var.azure_location
}
