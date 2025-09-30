# Modern Terraform Infrastructure Platform

This directory contains the modernized Infrastructure as Code (IaC) implementation for the Real-Time Financial Monitoring system, implementing enterprise-grade platform engineering patterns.

## Architecture Overview

### Platform Structure
```
platform/
├── terraform/
│   ├── environments/          # Environment-specific configurations
│   │   ├── dev/              # Development environment
│   │   └── prod/             # Production environment
│   ├── modules/              # Reusable Terraform modules
│   │   ├── aks/             # Azure Kubernetes Service
│   │   ├── cosmos/          # Cosmos DB
│   │   ├── security/        # Key Vault & Security
│   │   └── monitoring/      # Application Insights & Log Analytics
│   └── shared/              # Shared infrastructure (ACR, Terraform state)
├── deploy.sh                # Deployment automation script
```

## Quick Start

### Prerequisites
- Azure CLI logged in and configured
- Terraform >= 1.0 installed
- Appropriate Azure permissions (Contributor + User Access Administrator)

### Deployment Order

1. **Deploy Shared Infrastructure First**
   ```bash
   ./deploy.sh -e shared
   ```

2. **Deploy Development Environment**
   ```bash
   # Initialize and configure
   ./deploy.sh -e dev -i

   # Update terraform.tfvars with your admin_user_object_id
   # Then deploy
   ./deploy.sh -e dev
   ```

3. **Deploy Production Environment**
   ```bash
   ./deploy.sh -e prod -i
   # Update terraform.tfvars, then deploy
   ./deploy.sh -e prod
   ```

### Configuration

Before deploying any environment, update the `terraform.tfvars` file:

```hcl
# Required: Get your Object ID from Azure AD
admin_user_object_id = "your-azure-ad-object-id-here"

# Optional: Customize other settings
azure_location = "mexicocentral"
resource_prefix = "finmon"
enable_cost_optimization = true
development_mode = true  # dev only
alert_email_addresses = ["admin@yourcompany.com"]
```

