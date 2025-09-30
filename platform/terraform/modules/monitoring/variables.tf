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

variable "alert_email_addresses" {
  description = "List of email addresses for alert notifications"
  type        = list(string)
  default     = []
}

variable "enable_alerting" {
  description = "Enable monitoring alerts"
  type        = bool
  default     = true
}

variable "aks_cluster_id" {
  description = "The ID of the AKS cluster for monitoring"
  type        = string
  default     = ""
}