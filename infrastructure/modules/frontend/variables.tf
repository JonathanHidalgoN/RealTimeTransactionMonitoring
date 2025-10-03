variable "environment" {
  description = "Environment name (dev, prod)"
  type        = string
}

variable "resource_prefix" {
  description = "Prefix for resource names"
  type        = string
}

variable "location" {
  description = "Azure region (note: Static Web Apps has limited region support)"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "api_url" {
  description = "Backend API URL"
  type        = string
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
