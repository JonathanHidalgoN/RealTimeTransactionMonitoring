terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-tfstate-rg"
    storage_account_name = "finmontfstate"
    container_name       = "tfstate"
    key                  = "dev.terraform.tfstate"
  }
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 3.0" }
    azuread = { source = "hashicorp/azuread", version = "~> 2.0" }
  }
}

provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
}

data "azurerm_client_config" "current" {}

module "security" {
  source = "../../modules/security"

  environment          = "dev"
  resource_prefix      = var.resource_prefix
  azure_location       = var.azure_location
  admin_user_object_id = var.admin_user_object_id
  development_mode     = var.development_mode
}

module "cosmos_db" {
  source = "../../modules/cosmos"

  environment      = "dev"
  resource_prefix  = var.resource_prefix
  azure_location   = var.azure_location
  development_mode = var.development_mode
}

module "aks_cluster" {
  source = "../../modules/aks"

  environment     = "dev"
  resource_prefix = var.resource_prefix
  azure_location  = var.azure_location

  admin_user_object_id     = var.admin_user_object_id
  enable_cost_optimization = var.enable_cost_optimization
  development_mode         = var.development_mode
  key_vault_id            = module.security.key_vault_id
}

module "monitoring" {
  source = "../../modules/monitoring"

  environment       = "dev"
  resource_prefix   = var.resource_prefix
  azure_location    = var.azure_location
  aks_cluster_id    = module.aks_cluster.cluster_id
  enable_alerting   = var.enable_alerting
  alert_email_addresses = var.alert_email_addresses
}