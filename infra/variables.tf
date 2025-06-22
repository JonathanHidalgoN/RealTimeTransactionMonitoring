variable "azure_location" {
  description = "The Azure region where resources will be created."
  type        = string
  default     = "mexicocentral"
}

variable "resource_prefix" {
  description = "A unique prefix for naming resources."
  type        = string
  default     = "finmon"
}

variable "admin_user_object_id" {
  description = "The Object ID of the administrator user to grant Key Vault admin rights."
  type        = string
}
