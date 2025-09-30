resource "azurerm_resource_group" "monitoring" {
  name     = "${var.resource_prefix}-monitoring-${var.environment}-rg"
  location = var.azure_location

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = "${var.resource_prefix}-law-${var.environment}"
  location            = azurerm_resource_group.monitoring.location
  resource_group_name = azurerm_resource_group.monitoring.name
  sku                 = "PerGB2018"
  retention_in_days   = var.environment == "prod" ? 90 : 30

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_application_insights" "main" {
  name                = "${var.resource_prefix}-appi-${var.environment}"
  location            = azurerm_resource_group.monitoring.location
  resource_group_name = azurerm_resource_group.monitoring.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_monitor_action_group" "main" {
  name                = "${var.resource_prefix}-alerts-${var.environment}"
  resource_group_name = azurerm_resource_group.monitoring.name
  short_name          = "finmon-ag"

  dynamic "email_receiver" {
    for_each = var.alert_email_addresses
    content {
      name          = "email-${email_receiver.key}"
      email_address = email_receiver.value
    }
  }

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}

resource "azurerm_monitor_metric_alert" "high_cpu" {
  count               = var.enable_alerting ? 1 : 0
  name                = "${var.resource_prefix}-high-cpu-${var.environment}"
  resource_group_name = azurerm_resource_group.monitoring.name
  scopes              = [var.aks_cluster_id]
  description         = "High CPU usage detected"

  criteria {
    metric_namespace = "Microsoft.ContainerService/managedClusters"
    metric_name      = "node_cpu_usage_percentage"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 80
  }

  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }

  tags = {
    Environment = var.environment
    Project     = "RealTimeFinancialMonitoring"
  }
}