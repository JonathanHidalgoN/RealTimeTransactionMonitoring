terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-rg"
    storage_account_name = "finmontfstate5a9syt"
    container_name       = "tfstate"
    key                  = "realtimefinancialmonitoring.terraform.tfstate"
  }
}
