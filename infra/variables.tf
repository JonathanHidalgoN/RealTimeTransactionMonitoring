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

variable "deployment_architecture" {
  description = "Choose deployment architecture: 'aks' or 'containerapp'"
  type        = string
  default     = "aks"
  validation {
    condition     = contains(["aks", "containerapp"], var.deployment_architecture)
    error_message = "Deployment architecture must be either 'aks' or 'containerapp'."
  }
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
