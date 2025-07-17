resource "azurerm_redis_cache" "cache" {
  count                = var.anomaly_detection_mode == "stateful" ? 1 : 0
  name                 = "${var.resource_prefix}-redis-cache-${random_id.suffix.hex}"
  location             = azurerm_resource_group.rg.location
  resource_group_name  = azurerm_resource_group.rg.name
  capacity             = 0
  family               = "C"
  sku_name             = "Basic"
  non_ssl_port_enabled = false
  minimum_tls_version  = "1.2"

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}
