variable "resource_prefix" {
  description = "Prefix for all resource names"
  type        = string
  default     = "finmon"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "mexicocentral"
}

variable "admin_user_object_id" {
  description = "Object ID of admin user for Key Vault access (get with: az ad signed-in-user show --query id -o tsv)"
  type        = string
}
