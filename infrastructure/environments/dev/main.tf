data "azurerm_client_config" "current" {}

data "terraform_remote_state" "shared" {
  backend = "azurerm"
  config = {
    resource_group_name  = "finmon-tfstate-rg"
    storage_account_name = "finmontfstate"
    container_name       = "tfstate"
    key                  = "shared.terraform.tfstate"
  }
}

resource "azurerm_resource_group" "main" {
  name     = "${var.resource_prefix}-dev-rg"
  location = var.location

  tags = local.common_tags
}

locals {
  environment = "dev"
  common_tags = {
    Environment = "dev"
    Project     = "RealTimeFinancialMonitoring"
    ManagedBy   = "Terraform"
  }
}

module "monitoring" {
  source = "../../modules/monitoring"

  environment         = local.environment
  resource_prefix     = var.resource_prefix
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

module "security" {
  source = "../../modules/security"

  environment          = local.environment
  resource_prefix      = var.resource_prefix
  location             = var.location
  resource_group_name  = azurerm_resource_group.main.name
  admin_user_object_id = var.admin_user_object_id
  tags                 = local.common_tags
}

module "data" {
  source = "../../modules/data"

  environment         = local.environment
  resource_prefix     = var.resource_prefix
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

module "container_apps" {
  source = "../../modules/container-apps"

  environment                    = local.environment
  resource_prefix                = var.resource_prefix
  location                       = var.location
  resource_group_name            = azurerm_resource_group.main.name
  log_analytics_workspace_id     = module.monitoring.log_analytics_workspace_id
  acr_login_server               = data.terraform_remote_state.shared.outputs.acr_login_server
  acr_admin_username             = data.terraform_remote_state.shared.outputs.acr_admin_username
  acr_admin_password             = data.terraform_remote_state.shared.outputs.acr_admin_password
  cosmos_endpoint                = module.data.cosmos_endpoint
  cosmos_primary_key             = module.data.cosmos_primary_key
  cosmos_database_name           = module.data.cosmos_database_name
  cosmos_container_name          = module.data.cosmos_container_name
  cosmos_partition_key_path      = module.data.cosmos_partition_key_path
  eventhub_connection_string     = module.data.eventhub_connection_string
  storage_connection_string      = module.data.storage_connection_string
  app_insights_connection_string = module.monitoring.app_insights_connection_string
  key_vault_uri                  = module.security.key_vault_uri
  managed_identity_id            = module.security.managed_identity_id
  managed_identity_client_id     = module.security.managed_identity_client_id
  tags                           = local.common_tags
}

module "frontend" {
  source = "../../modules/frontend"

  environment         = local.environment
  resource_prefix     = var.resource_prefix
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  api_url             = module.container_apps.api_url
  tags                = local.common_tags
}
