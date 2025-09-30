variable "environment" {
  description = "Environment name (dev, prod)"
  type        = string
}

variable "resource_prefix" {
  description = "A unique prefix for naming resources"
  type        = string
}

variable "azure_location" {
  description = "The Azure region where resources will be created"
  type        = string
}

variable "admin_user_object_id" {
  description = "The Object ID of the administrator user"
  type        = string
}

variable "enable_cost_optimization" {
  description = "Enable cost optimization features"
  type        = bool
  default     = true
}

variable "development_mode" {
  description = "Enable development mode"
  type        = bool
  default     = true
}

variable "key_vault_id" {
  description = "The ID of the Key Vault for workload identity RBAC"
  type        = string
  default     = ""
}