variable "environment" {
  description = "Environment name (dev, prod)"
  type        = string
}

variable "resource_prefix" {
  description = "Prefix for resource names"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics Workspace ID for Container Apps Environment"
  type        = string
}

variable "acr_login_server" {
  description = "Azure Container Registry login server"
  type        = string
}

variable "acr_admin_username" {
  description = "Azure Container Registry admin username"
  type        = string
}

variable "acr_admin_password" {
  description = "Azure Container Registry admin password"
  type        = string
  sensitive   = true
}

variable "cosmos_connection_string" {
  description = "Cosmos DB connection string"
  type        = string
  sensitive   = true
}

variable "eventhub_connection_string" {
  description = "Event Hubs connection string"
  type        = string
  sensitive   = true
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string"
  type        = string
  sensitive   = true
}

variable "storage_connection_string" {
  description = "Storage account connection string for Event Hubs checkpoints"
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
