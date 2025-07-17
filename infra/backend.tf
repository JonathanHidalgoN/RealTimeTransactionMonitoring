terraform {
  backend "azurerm" {
    resource_group_name  = "finmon"
    storage_account_name = "finmontfstate45qna2"
    container_name       = "tfstate"
    key                  = "realtimefinancialmonitoring.terraform.tfstate"
  }
}
