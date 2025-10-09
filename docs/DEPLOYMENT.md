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

## Step 6: Deploy Frontend

The frontend is a Blazor WebAssembly application that runs entirely in the browser. It needs to be:
1. Built as static files (HTML, CSS, JS, WebAssembly)
2. Configured with the API URL and API Key
3. Deployed to Azure Static Web Apps

### Prerequisites

Install the Azure Static Web Apps CLI:

```bash
npm install -g @azure/static-web-apps-cli
```

### Manual Deployment Steps

1. **Get configuration from Terraform:**

```bash
cd infrastructure/environments/dev
API_URL=$(terraform output -raw api_url)
API_KEY=$(terraform output -raw api_key)
DEPLOYMENT_TOKEN=$(terraform output -raw frontend_api_key)
```

2. **Build the Blazor WebAssembly app:**

```bash
cd ../../src/FinancialMonitoring.WebApp
dotnet publish -c Release -o ../../build/webapp
```

3. **Replace configuration placeholders:**

The source code contains placeholders `__ApiBaseUrl__` and `__ApiKey__` that need to be replaced with actual values:

```bash
cd ../../build/webapp/wwwroot
sed -i "s|__ApiBaseUrl__|$API_URL|g" appsettings.json
sed -i "s|__ApiKey__|$API_KEY|g" appsettings.json
```

4. **Deploy to Azure Static Web Apps:**

```bash
swa deploy . --deployment-token "$DEPLOYMENT_TOKEN" --env production
```

### Automated Deployment

For convenience, all the above steps are automated in a single script:

```bash
./scripts/deploy-frontend.sh dev
```

### Verify Frontend


```bash
cd infrastructure/environments/dev
terraform output frontend_url
```

Open the URL in your browser. The application should load and be able to fetch data from the API.
