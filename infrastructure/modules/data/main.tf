resource "azurerm_cosmosdb_account" "main" {
  name                = "${var.resource_prefix}-cosmos-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  capabilities {
    name = "EnableServerless"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }

  tags = var.tags
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "FinancialMonitoring"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
}

resource "azurerm_cosmosdb_sql_container" "transactions" {
  name                = "Transactions"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_path  = "/id"
}

resource "azurerm_eventhub_namespace" "main" {
  name                = "${var.resource_prefix}-ehns-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Basic"
  capacity            = 1

  tags = var.tags
}

resource "azurerm_eventhub" "transactions" {
  name                = "transactions"
  namespace_name      = azurerm_eventhub_namespace.main.name
  resource_group_name = var.resource_group_name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_eventhub" "anomalies" {
  name                = "anomalies"
  namespace_name      = azurerm_eventhub_namespace.main.name
  resource_group_name = var.resource_group_name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_storage_account" "eventhub_checkpoints" {
  name                     = "${var.resource_prefix}ehckpt${var.environment}${random_string.suffix.result}"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = var.tags
}

resource "azurerm_storage_container" "checkpoints" {
  name                  = "eh-checkpoints"
  storage_account_name  = azurerm_storage_account.eventhub_checkpoints.name
  container_access_type = "private"
}

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}
