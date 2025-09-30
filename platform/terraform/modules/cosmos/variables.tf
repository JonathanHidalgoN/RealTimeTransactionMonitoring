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

variable "development_mode" {
  description = "Enable development mode (reduced redundancy for cost savings)"
  type        = bool
  default     = true
}

variable "secondary_location" {
  description = "Secondary Azure region for geo-replication (prod only)"
  type        = string
  default     = "eastus2"
}