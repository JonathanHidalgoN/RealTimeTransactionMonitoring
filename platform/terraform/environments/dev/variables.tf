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

variable "enable_cost_optimization" {
  description = "Enable cost optimization features (smaller VMs, scale to zero)"
  type        = bool
  default     = true
}

variable "development_mode" {
  description = "Enable development mode (reduced redundancy for cost savings)"
  type        = bool
  default     = true
}

variable "enable_alerting" {
  description = "Enable monitoring alerts"
  type        = bool
  default     = true
}

variable "alert_email_addresses" {
  description = "List of email addresses for alert notifications"
  type        = list(string)
  default     = []
}