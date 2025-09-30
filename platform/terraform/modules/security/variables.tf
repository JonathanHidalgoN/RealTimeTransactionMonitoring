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
  description = "The Object ID of the administrator user to grant Key Vault admin rights"
  type        = string
}

variable "development_mode" {
  description = "Enable development mode (reduced security for cost savings)"
  type        = bool
  default     = true
}

variable "aks_identity_principal_id" {
  description = "The principal ID of the AKS managed identity"
  type        = string
  default     = ""
}

variable "cosmos_connection_string" {
  description = "Cosmos DB connection string to store in Key Vault"
  type        = string
  default     = ""
  sensitive   = true
}

variable "cosmos_primary_key" {
  description = "Cosmos DB primary key to store in Key Vault"
  type        = string
  default     = ""
  sensitive   = true
}