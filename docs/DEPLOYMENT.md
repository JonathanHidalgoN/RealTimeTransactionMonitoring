# Deployment Guide

This guide walks you through deploying the Real-Time Financial Monitoring application to Azure.

## Overview

The deployment process involves:
1. Setting up Terraform backend (Azure Storage)
2. Authenticating with Azure
3. Deploying shared infrastructure (ACR)
4. Building and pushing Docker images
5. Deploying environment-specific resources

We've automated most of this with scripts, so deployment is straightforward.

## Prerequisites

Before deploying, ensure you have:

- **Azure CLI**
- **Terraform** (>= 1.5)
- **Docker**
- **Azure Subscription** with appropriate permissions (Contributor role minimum)

## Step 1: Authenticate with Azure

Login to your Azure account:

```bash
az login
```

Verify you're using the correct subscription:

```bash
az account show
```

## Step 2: Bootstrap Terraform Backend

Terraform needs a backend to store its state files. We use Azure Storage for this.

Run the bootstrap script (only needed once per Azure subscription):

```bash
./scripts/bootstrap-terraform.sh
```

This creates:
- Resource Group: `finmon-tfstate-rg`
- Storage Account: `finmontfstate`
- Blob Container: `tfstate`

## Step 3: Configure Environment Variables

Before deploying, you need to populate `terraform.tfvars` for your environment.

### Get Your Azure AD User Object ID
### Create/Update terraform.tfvars

Edit `infrastructure/environments/dev/terraform.tfvars`:

```hcl
resource_prefix      = "finmon"
location            = "centralus"  # or your preferred Azure region
admin_user_object_id = "your-user-object-id-from-above"
```

## Step 4: Deploy Everything

Run the deployment script:

```bash
./scripts/deploy.sh dev
```

This script orchestrates the full deployment in 3 phases:

### Phase 1: Shared Infrastructure
- Creates Azure Container Registry (ACR)
- Runs `terraform init` and `terraform apply` in `infrastructure/shared/`

### Phase 2: Build and Push Images
- Builds Docker images for:
  - API (FinancialMonitoring.Api)
  - Transaction Processor
  - Transaction Simulator
- Pushes images to ACR

### Phase 3: Environment Deployment
- Creates environment resources:
  - Cosmos DB
  - Event Hubs
  - Key Vault with auto-generated API key
  - Container Apps (API, Processor, Simulator)
  - Application Insights
- Runs `terraform init` and `terraform apply` in `infrastructure/environments/dev/`

**Expected time:** 10-15 minutes

## Step 5: Verify Deployment

After deployment completes, verify the infrastructure:

### Check Resource Groups

```bash
az group list --output table | grep finmon
```

You should see:
- `finmon-tfstate-rg` (Terraform backend)
- `finmon-shared-rg` (ACR)
- `finmon-dev-rg` (Application resources)

### Check Container Apps

```bash
az containerapp list --resource-group finmon-dev-rg --output table
```

You should see 3 apps running:
- `finmon-api-dev`
- `finmon-processor-dev`
- `finmon-simulator-dev`

### Test the API

Get the API URL:

```bash
az containerapp show \
  --name finmon-api-dev \
  --resource-group finmon-dev-rg \
  --query properties.configuration.ingress.fqdn -o tsv
```

Test the health endpoint:

```bash
curl https://<api-fqdn>/health
```
