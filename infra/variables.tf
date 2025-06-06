variable "azure_location" {
  description = "The Azure region where resources will be created."
  type        = string
  default     = "Mexico Central"
}

variable "resource_prefix" {
  description = "A unique prefix for naming resources."
  type        = string
  default     = "prealtimef"
}

variable "app_service_principal_object_id" {
  description = "The Object ID of the Service Principal used by the application."
  type        = string
}
