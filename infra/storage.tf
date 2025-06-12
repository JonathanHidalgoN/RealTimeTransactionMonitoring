resource "azurerm_storage_account" "eh_checkpoints" {
  name                     = "${var.resource_prefix}ehcp${random_id.suffix.hex}"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "eh_checkpoints" {
  name                  = "eh-checkpoints"
  storage_account_name  = azurerm_storage_account.eh_checkpoints.name
  container_access_type = "private"
}
