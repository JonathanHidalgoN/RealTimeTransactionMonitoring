terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-rg"
    storage_account_name = "finmontfstatesaxcf6"
    container_name       = "tfstate"
    key                  = "realtimefinancialmonitoring.terraform.tfstate"
  }
}
