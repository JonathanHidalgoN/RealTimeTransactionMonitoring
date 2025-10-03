terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-tfstate-rg"
    storage_account_name = "finmontfstate"
    container_name       = "tfstate"
    key                  = "shared.terraform.tfstate"
  }
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

resource "azurerm_resource_group" "shared" {
  name     = "${var.resource_prefix}-shared-rg"
  location = var.location

  tags = var.tags
}

resource "azurerm_container_registry" "main" {
  name                = "${var.resource_prefix}acr${random_string.suffix.result}"
  resource_group_name = azurerm_resource_group.shared.name
  location            = azurerm_resource_group.shared.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = var.tags
}

resource "random_string" "suffix" {
  length  = 8
  special = false
  upper   = false
}
