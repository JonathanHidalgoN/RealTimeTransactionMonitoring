variable "resource_prefix" {
  description = "Prefix for all resource names"
  type        = string
  default     = "finmon"
}

variable "location" {
  description = "Azure region for shared resources"
  type        = string
  default     = "mexicocentral"
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Project   = "RealTimeFinancialMonitoring"
    Purpose   = "Shared Resources"
    ManagedBy = "Terraform"
  }
}
