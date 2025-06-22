terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-rg"
    storage_account_name = "finmontfstate55mbn8"
    container_name       = "tfstate"
    key                  = "realtimefinancialmonitoring.terraform.tfstate"
  }
}
