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