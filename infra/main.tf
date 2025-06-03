terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {} # Recommended default block
}

variable "azure_location" {
  description = "The Azure region where resources will be created."
  type        = string
  default     = "Mexico Central"
}

variable "resource_prefix" {
  description = "A unique prefix for naming resources."
  type        = string
  default     = "prealtimef"
}

# 1.Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "${var.resource_prefix}-appinsights-rg"
  location = var.azure_location
}

# 2.Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "law" {
  name                = "${var.resource_prefix}-appinsights-law"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018" # Standard SKU
  retention_in_days   = 30
}

# 3.Application Insights
resource "azurerm_application_insights" "appi" {
  name                = "${var.resource_prefix}-appinsights"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  workspace_id        = azurerm_log_analytics_workspace.law.id
  application_type    = "web"

  tags = {
    environment = "development"
    project     = "RealTimeFinancialMonitoring"
  }
}

output "application_insights_connection_string" {
  description = "The Connection String for Application Insights."
  value       = azurerm_application_insights.appi.connection_string
  sensitive   = true
}
