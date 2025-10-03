terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-tfstate-rg"
    storage_account_name = "finmontfstate"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = true
    }
  }
}
