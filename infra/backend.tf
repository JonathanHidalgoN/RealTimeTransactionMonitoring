terraform {
  backend "azurerm" {
    resource_group_name  = "finmon-rg"
    storage_account_name = "finmontfstate2f0vu6"
    container_name       = "tfstate"
    key                  = "realtimefinancialmonitoring.terraform.tfstate"
  }
}
