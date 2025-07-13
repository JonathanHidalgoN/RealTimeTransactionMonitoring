resource "azurerm_container_app_environment" "main" {
  count                      = var.deployment_architecture == "containerapp" ? 1 : 0
  name                       = "${var.resource_prefix}-ca-env-${random_id.suffix.hex}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
    architecture = "containerapp"
  }
}

# API Container App
resource "azurerm_container_app" "api" {
  count                        = var.deployment_architecture == "containerapp" ? 1 : 0
  name                         = "${var.resource_prefix}-api-${random_id.suffix.hex}"
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode               = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  template {
    min_replicas = var.development_mode ? 0 : 1
    max_replicas = var.development_mode ? 2 : 5

    container {
      name   = "api"
      image  = "${azurerm_container_registry.acr.login_server}/financialmonitoring-api:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "CosmosDb__ConnectionString"
        value = azurerm_cosmosdb_account.db.primary_sql_connection_string
      }

      env {
        name  = "EventHub__ConnectionString"
        value = azurerm_eventhub_authorization_rule.transactions_rule.primary_connection_string
      }

      env {
        name  = "Redis__ConnectionString"
        value = "${azurerm_redis_cache.cache.hostname}:${azurerm_redis_cache.cache.ssl_port},password=${azurerm_redis_cache.cache.primary_access_key},ssl=True,abortConnect=False"
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = azurerm_application_insights.appinsights.connection_string
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 80

    traffic_policy {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}

# Transaction Processor Container App
resource "azurerm_container_app" "processor" {
  count                        = var.deployment_architecture == "containerapp" ? 1 : 0
  name                         = "${var.resource_prefix}-processor-${random_id.suffix.hex}"
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode               = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  template {
    min_replicas = var.development_mode ? 0 : 1
    max_replicas = var.development_mode ? 2 : 3

    container {
      name   = "processor"
      image  = "${azurerm_container_registry.acr.login_server}/transactionprocessor:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "CosmosDb__ConnectionString"
        value = azurerm_cosmosdb_account.db.primary_sql_connection_string
      }

      env {
        name  = "EventHub__ConnectionString"
        value = azurerm_eventhub_authorization_rule.transactions_rule.primary_connection_string
      }

      env {
        name  = "Redis__ConnectionString"
        value = "${azurerm_redis_cache.cache.hostname}:${azurerm_redis_cache.cache.ssl_port},password=${azurerm_redis_cache.cache.primary_access_key},ssl=True,abortConnect=False"
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = azurerm_application_insights.appinsights.connection_string
      }
    }
  }

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}

# Transaction Simulator Container App
resource "azurerm_container_app" "simulator" {
  count                        = var.deployment_architecture == "containerapp" ? 1 : 0
  name                         = "${var.resource_prefix}-simulator-${random_id.suffix.hex}"
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = azurerm_resource_group.rg.name
  revision_mode               = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app_identity.id]
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "simulator"
      image  = "${azurerm_container_registry.acr.login_server}/transactionsimulator:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "EventHub__ConnectionString"
        value = azurerm_eventhub_authorization_rule.transactions_rule.primary_connection_string
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = azurerm_application_insights.appinsights.connection_string
      }
    }
  }

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
