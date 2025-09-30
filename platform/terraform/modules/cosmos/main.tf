resource "azurerm_resource_group" "cosmos" {
  name     = "${var.resource_prefix}-cosmos-${var.environment}-rg"
  location = var.azure_location

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_cosmosdb_account" "main" {
  name                = "${var.resource_prefix}-cosmos-${var.environment}"
  location            = azurerm_resource_group.cosmos.location
  resource_group_name = azurerm_resource_group.cosmos.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  free_tier_enabled = var.development_mode

  consistency_policy {
    consistency_level = var.development_mode ? "Session" : "BoundedStaleness"
    max_interval_in_seconds = var.development_mode ? null : 300
    max_staleness_prefix    = var.development_mode ? null : 100000
  }

  geo_location {
    location          = azurerm_resource_group.cosmos.location
    failover_priority = 0
  }

  dynamic "geo_location" {
    for_each = var.development_mode ? [] : [1]
    content {
      location          = var.secondary_location
      failover_priority = 1
    }
  }

  backup {
    type                = var.development_mode ? "Periodic" : "Continuous"
    interval_in_minutes = var.development_mode ? 1440 : 240
    retention_in_hours  = var.development_mode ? 168 : 720
  }

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "FinancialTransactionsDb"
  resource_group_name = azurerm_resource_group.cosmos.name
  account_name        = azurerm_cosmosdb_account.main.name

  throughput = var.development_mode ? 400 : 1000
}

resource "azurerm_cosmosdb_sql_container" "transactions" {
  name                = "Transactions"
  resource_group_name = azurerm_resource_group.cosmos.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/id"]

  throughput = var.development_mode ? 400 : 1000

  indexing_policy {
    indexing_mode = "consistent"

    included_path {
      path = "/*"
    }

    excluded_path {
      path = "/\"_etag\"/?"
    }
  }
}

resource "azurerm_cosmosdb_sql_container" "anomalies" {
  name                = "Anomalies"
  resource_group_name = azurerm_resource_group.cosmos.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths = ["/transactionId"]

  throughput = var.development_mode ? 400 : 800
}