terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

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

resource "azurerm_key_vault" "kv" {
  name                        = "${var.resource_prefix}-kv-${random_id.suffix.hex}"
  location                    = azurerm_resource_group.rg.location
  resource_group_name         = azurerm_resource_group.rg.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete"
    ]
  }

}

resource "random_id" "suffix" {
  byte_length = 4
}

output "application_insights_connection_string" {
  description = "The Connection String for Application Insights."
  value       = azurerm_application_insights.appi.connection_string
  sensitive   = true
}

output "key_vault_uri" {
  description = "The URI of the Azure Key Vault."
  value       = azurerm_key_vault.kv.vault_uri
  sensitive   = true
}
