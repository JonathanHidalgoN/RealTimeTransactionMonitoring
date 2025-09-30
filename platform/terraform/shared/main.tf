terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-tfstate-rg"
    storage_account_name = "finmontfstate"
    container_name       = "tfstate"
    key                  = "shared.terraform.tfstate"
  }
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 3.0" }
  }
}

provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
}

resource "azurerm_resource_group" "shared" {
  name     = "${var.resource_prefix}-shared-rg"
  location = var.azure_location

  tags = {
    Project = "RealTimeFinancialMonitoring"
    Purpose = "Shared Resources"
  }
}

resource "azurerm_container_registry" "main" {
  name                = "${var.resource_prefix}acr${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.shared.name
  location            = azurerm_resource_group.shared.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = {
    Project = "RealTimeFinancialMonitoring"
    Purpose = "Container Registry"
  }
}

resource "random_string" "suffix" {
  length  = 8
  special = false
  upper   = false
}

resource "azurerm_storage_account" "tfstate" {
  name                     = "${var.resource_prefix}tfstate${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.shared.name
  location                 = azurerm_resource_group.shared.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    Project = "RealTimeFinancialMonitoring"
    Purpose = "Terraform State"
  }
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_name  = azurerm_storage_account.tfstate.name
  container_access_type = "private"
}