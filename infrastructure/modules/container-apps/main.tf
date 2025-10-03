resource "azurerm_container_app_environment" "main" {
  name                       = "${var.resource_prefix}-ca-env-${var.environment}"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = var.log_analytics_workspace_id

  tags = var.tags
}

resource "azurerm_container_app" "api" {
  name                         = "${var.resource_prefix}-api-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = 0
    max_replicas = 3

    container {
      name   = "api"
      image  = "${var.acr_login_server}/financialmonitoring-api:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name  = "MongoDb__ConnectionString"
        value = var.cosmos_connection_string
      }

      env {
        name  = "MongoDb__DatabaseName"
        value = "FinancialMonitoring"
      }

      env {
        name  = "MongoDb__CollectionName"
        value = "Transactions"
      }

      env {
        name  = "EventHubs__ConnectionString"
        value = var.eventhub_connection_string
      }

      env {
        name  = "EventHubs__EventHubName"
        value = "transactions"
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = var.app_insights_connection_string
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  registry {
    server               = var.acr_login_server
    username             = var.acr_admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = var.acr_admin_password
  }

  tags = var.tags
}

resource "azurerm_container_app" "processor" {
  name                         = "${var.resource_prefix}-processor-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = 1
    max_replicas = 3

    container {
      name   = "processor"
      image  = "${var.acr_login_server}/transactionprocessor:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "MongoDb__ConnectionString"
        value = var.cosmos_connection_string
      }

      env {
        name  = "MongoDb__DatabaseName"
        value = "FinancialMonitoring"
      }

      env {
        name  = "MongoDb__CollectionName"
        value = "Transactions"
      }

      env {
        name  = "EventHubs__ConnectionString"
        value = var.eventhub_connection_string
      }

      env {
        name  = "EventHubs__EventHubName"
        value = "transactions"
      }

      env {
        name  = "EventHubs__BlobContainerName"
        value = "eh-checkpoints"
      }

      env {
        name  = "Messaging__Provider"
        value = "eventhubs"
      }

      env {
        name  = "AnomalyDetection__Mode"
        value = "stateless"
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = var.app_insights_connection_string
      }

      env {
        name  = "AzureWebJobsStorage"
        value = var.storage_connection_string
      }
    }
  }

  registry {
    server               = var.acr_login_server
    username             = var.acr_admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = var.acr_admin_password
  }

  tags = var.tags
}

resource "azurerm_container_app" "simulator" {
  name                         = "${var.resource_prefix}-simulator-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "simulator"
      image  = "${var.acr_login_server}/transactionsimulator:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "EventHubs__ConnectionString"
        value = var.eventhub_connection_string
      }

      env {
        name  = "EventHubs__EventHubName"
        value = "transactions"
      }

      env {
        name  = "Messaging__Provider"
        value = "eventhubs"
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = var.app_insights_connection_string
      }
    }
  }

  registry {
    server               = var.acr_login_server
    username             = var.acr_admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = var.acr_admin_password
  }

  tags = var.tags
}
