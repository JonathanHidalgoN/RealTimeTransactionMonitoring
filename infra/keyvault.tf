resource "random_id" "suffix" {
  byte_length = 4
}

resource "azurerm_key_vault" "kv" {
  name                       = "${var.resource_prefix}-kv-${random_id.suffix.hex}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days = 7
  purge_protection_enabled   = false
  sku_name                   = "standard"

  enable_rbac_authorization = true
}
