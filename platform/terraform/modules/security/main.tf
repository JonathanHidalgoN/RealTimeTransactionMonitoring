data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "security" {
  name     = "${var.resource_prefix}-security-${var.environment}-rg"
  location = var.azure_location

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_key_vault" "main" {
  name                       = "${var.resource_prefix}-kv-${var.environment}"
  location                   = azurerm_resource_group.security.location
  resource_group_name        = azurerm_resource_group.security.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days = var.development_mode ? 7 : 90
  purge_protection_enabled   = var.development_mode ? false : true
  sku_name                   = "standard"

  enable_rbac_authorization = true

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_role_assignment" "admin_key_vault" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = var.admin_user_object_id
}

resource "azurerm_role_assignment" "aks_key_vault_secrets" {
  count                = var.aks_identity_principal_id != "" ? 1 : 0
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = var.aks_identity_principal_id
}

resource "azurerm_key_vault_secret" "cosmos_connection_string" {
  count        = var.cosmos_connection_string != "" ? 1 : 0
  name         = "cosmos-connection-string"
  value        = var.cosmos_connection_string
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [azurerm_role_assignment.admin_key_vault]
}

resource "azurerm_key_vault_secret" "cosmos_primary_key" {
  count        = var.cosmos_primary_key != "" ? 1 : 0
  name         = "cosmos-primary-key"
  value        = var.cosmos_primary_key
  key_vault_id = azurerm_key_vault.main.id

  depends_on = [azurerm_role_assignment.admin_key_vault]
}