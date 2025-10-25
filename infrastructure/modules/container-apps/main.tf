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
    type         = "UserAssigned"
    identity_ids = [var.managed_identity_id]
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
        name  = "DOTNET_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name  = "CosmosDb__EndpointUri"
        value = var.cosmos_endpoint
      }

      env {
        name       = "CosmosDb__PrimaryKey"
        secret_name = "cosmos-primary-key"
      }

      env {
        name  = "CosmosDb__DatabaseName"
        value = var.cosmos_database_name
      }

      env {
        name  = "CosmosDb__ContainerName"
        value = var.cosmos_container_name
      }

      env {
        name  = "CosmosDb__PartitionKeyPath"
        value = var.cosmos_partition_key_path
      }

      env {
        name       = "ApplicationInsights__ConnectionString"
        secret_name = "app-insights-connection-string"
      }

      env {
        name  = "KEY_VAULT_URI"
        value = var.key_vault_uri
      }

      env {
        name  = "AZURE_CLIENT_ID"
        value = var.managed_identity_client_id
      }

      env {
        name  = "Cors__AllowedOrigins__0"
        value = var.frontend_url
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

  secret {
    name  = "cosmos-primary-key"
    value = var.cosmos_primary_key
  }

  secret {
    name  = "app-insights-connection-string"
    value = var.app_insights_connection_string
  }

  tags = var.tags
}

resource "azurerm_container_app" "processor" {
  name                         = "${var.resource_prefix}-processor-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [var.managed_identity_id]
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
        name  = "CosmosDb__EndpointUri"
        value = var.cosmos_endpoint
      }

      env {
        name       = "CosmosDb__PrimaryKey"
        secret_name = "cosmos-primary-key"
      }

      env {
        name  = "CosmosDb__DatabaseName"
        value = var.cosmos_database_name
      }

      env {
        name  = "CosmosDb__ContainerName"
        value = var.cosmos_container_name
      }

      env {
        name  = "CosmosDb__PartitionKeyPath"
        value = var.cosmos_partition_key_path
      }

      env {
        name       = "EventHubs__ConnectionString"
        secret_name = "eventhub-connection-string"
      }

      env {
        name  = "EventHubs__EventHubName"
        value = "transactions"
      }

      env {
        name       = "EventHubs__BlobStorageConnectionString"
        secret_name = "storage-connection-string"
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
        name       = "ApplicationInsights__ConnectionString"
        secret_name = "app-insights-connection-string"
      }

      env {
        name       = "AzureWebJobsStorage"
        secret_name = "storage-connection-string"
      }

      env {
        name  = "KEY_VAULT_URI"
        value = var.key_vault_uri
      }

      env {
        name  = "AZURE_CLIENT_ID"
        value = var.managed_identity_client_id
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

  secret {
    name  = "cosmos-primary-key"
    value = var.cosmos_primary_key
  }

  secret {
    name  = "eventhub-connection-string"
    value = var.eventhub_connection_string
  }

  secret {
    name  = "storage-connection-string"
    value = var.storage_connection_string
  }

  secret {
    name  = "app-insights-connection-string"
    value = var.app_insights_connection_string
  }

  tags = var.tags
}

resource "azurerm_container_app" "simulator" {
  name                         = "${var.resource_prefix}-simulator-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [var.managed_identity_id]
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
        name       = "EventHubs__ConnectionString"
        secret_name = "eventhub-connection-string"
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
        name       = "ApplicationInsights__ConnectionString"
        secret_name = "app-insights-connection-string"
      }

      env {
        name  = "KEY_VAULT_URI"
        value = var.key_vault_uri
      }

      env {
        name  = "AZURE_CLIENT_ID"
        value = var.managed_identity_client_id
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

  secret {
    name  = "eventhub-connection-string"
    value = var.eventhub_connection_string
  }

  secret {
    name  = "app-insights-connection-string"
    value = var.app_insights_connection_string
  }

  tags = var.tags
}
