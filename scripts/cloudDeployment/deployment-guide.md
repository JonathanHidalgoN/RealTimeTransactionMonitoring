#Real-Time Financial Monitoring - Complete Deployment Guide

This guide will take you from a fresh Azure subscription to a fully working Real-Time Financial Monitoring application with transactions flowing.

## Prerequisites

1. **Azure CLI installed and logged in**
   ```bash
   az login
   az account set --subscription "your-subscription-id"
   ```

2. **Required tools installed**
   - Docker
   - kubectl
   - terraform
   - Azure Static Web Apps CLI: `npm install -g @azure/static-web-apps-cli`

3. **Environment file**: Create `.env` in project root with:
   ```
   AZURE_SUBSCRIPTION_ID="your-subscription-id"
   AZURE_LOCATION="eastus"
   RESOURCE_GROUP_NAME="finmon-rg"
   SERVICE_PRINCIPAL_NAME="FinMonInfraAppSP"
   TF_STATE_STORAGE_ACCOUNT_NAME_BASE="finmontfstate"
   API_KEY="your-secure-api-key"
   ```

## Deployment Steps

### 1. Bootstrap Azure Infrastructure
```bash
./scripts/cloudDeployment/bootstrap.sh
```
**What it does:** Creates resource groups, storage accounts, service principals, Terraform backend

**Output:** Creates `.terraform.env` file with service principal credentials

### 2. Deploy Infrastructure with Terraform
```bash
source .terraform.env
cd infra
terraform init -upgrade
terraform import azurerm_resource_group.rg "/subscriptions/YOUR-SUB-ID/resourceGroups/finmon-rg"
terraform plan -out=tfplan
terraform apply tfplan
cd ..
```
**What it does:** Creates AKS, ACR, Cosmos DB, Event Hubs, Key Vault, Redis, Static Web Apps

### 3. **CRITICAL MANUAL STEP** - Update ACR Names in Kubernetes Manifests
```bash
# Get the actual ACR name (includes random suffix)
ACR_NAME=$(cd infra && terraform output -raw acr_name)
echo "ACR Name: $ACR_NAME"

# Update these files manually:
# - k8s-manifest/03-api-deployment-service.yml
# - k8s-manifest/04-processor-deployment.yml
# - k8s-manifest/05-simulator-deployment.yml

# Replace image references from:
# image: finmonacr0aaa81f1.azurecr.io/financialmonitoring-api:latest
# To:
# image: $ACR_NAME.azurecr.io/financialmonitoring-api:latest
```

### 4. Setup Application Configuration
```bash
./scripts/cloudDeployment/setup_app_config.sh
```
**What it does:** Populates Key Vault with secrets, creates environment configuration

### 5. Install Kubernetes Infrastructure
```bash
# Connect to AKS cluster
RESOURCE_GROUP=$(cd infra && terraform output -raw resource_group_name)
AKS_NAME=$(cd infra && terraform output -raw aks_cluster_name)
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$AKS_NAME" --overwrite-existing

# Install NGINX Ingress Controller
./scripts/cloudDeployment/setup-ingress-controller.sh

# Install cert-manager for SSL
./scripts/cloudDeployment/install-cert-manager.sh
```
**Critical Output:** Note the LoadBalancer IP from ingress controller setup

### 6. Build and Push Container Images
```bash
./scripts/cloudDeployment/build-and-push-acr.sh
```
**What it does:** Builds and pushes API, TransactionProcessor, TransactionSimulator to ACR

### 7.**CRITICAL MANUAL STEP** - Update ConfigMap with Live Values
```bash
# Get values from Terraform
cd infra
COSMOS_URI=$(terraform output -raw cosmosdb_endpoint)
COSMOS_KEY=$(terraform output -raw cosmosdb_primary_key)
EH_CS=$(terraform output -raw eventhubs_namespace_connection_string)
AI_CS=$(terraform output -raw application_insights_connection_string)
cd ..

# Update ConfigMap
kubectl patch configmap env-config -n finmon-app --type merge -p "{
  \"data\": {
    \"CosmosDb__EndpointUri\": \"$COSMOS_URI\",
    \"CosmosDb__PrimaryKey\": \"$COSMOS_KEY\",
    \"EventHubs__ConnectionString\": \"$EH_CS\",
    \"ApplicationInsights__ConnectionString\": \"$AI_CS\"
  }
}"
```

### 8. Deploy Applications to Kubernetes
```bash
kubectl apply -k k8s-manifest/overlays/cloud/
```

### 9.**CRITICAL MANUAL STEP** - Update DNS
```bash
# Get the LoadBalancer IP
kubectl get service -n ingress-nginx

# Update your DNS provider (e.g., Namecheap) with:
# A Record: api.finmon-ui-azj.com -> EXTERNAL-IP-FROM-ABOVE
```

### 10. Deploy Blazor Frontend
```bash
./scripts/cloudDeployment/deploy-blazor-static-app.sh
```
**Output:** Provides Static Web App URL

### 11.**CRITICAL MANUAL STEP** - Update CORS Configuration
```bash
# Get Static Web App URL from previous step or:
STATIC_URL=$(cd infra && terraform output -raw static_web_app_default_hostname)

# Add to CORS configuration
kubectl patch configmap env-config -n finmon-app --type merge -p "{
  \"data\": {
    \"AllowedOrigins__0\": \"http://localhost:5124\",
    \"AllowedOrigins__1\": \"https://localhost:7258\",
    \"AllowedOrigins__2\": \"https://$STATIC_URL\"
  }
}"

# Restart API to pick up new CORS config
kubectl rollout restart deployment api-deployment -n finmon-app
```

## Verification ✅

### Check Application Status
```bash
# Verify all pods are running
kubectl get pods -n finmon-app

# Check ingress status
kubectl get ingress -n finmon-app

# Verify SSL certificate
kubectl get secrets api-tls-secret -n finmon-app
```

### Test Endpoints
```bash
# API Health Check
curl -k https://api.finmon-ui-azj.com/healthz

# Check if transactions are flowing
curl -k https://api.finmon-ui-azj.com/api/transactions
```

### Access Application
- **API:** https://api.finmon-ui-azj.com
- **Dashboard:** Your Static Web App URL (from step 10)

## Troubleshooting

### Debug Commands
```bash
# Check pod logs
kubectl logs -f deployment/api-deployment -n finmon-app
kubectl logs -f deployment/processor-deployment -n finmon-app
kubectl logs -f deployment/simulator-deployment -n finmon-app

# Check ingress controller
kubectl logs -f deployment/ingress-nginx-controller -n ingress-nginx

# Check cert-manager
kubectl logs -f deployment/cert-manager -n cert-manager
```

## Cost Management

```bash
# Stop cluster when not in use
./scripts/generalUtils/cost-management.sh stop

# Start for development work
./scripts/generalUtils/cost-management.sh work

# Scale for demo (2 nodes)
./scripts/generalUtils/cost-management.sh demo
```

## Summary

After completing all steps, you should have:
- AKS cluster with NGINX Ingress and SSL certificates
- API, TransactionProcessor, and TransactionSimulator running
- Blazor dashboard accessible via Static Web Apps
- Transactions flowing end-to-end
- HTTPS working with valid certificates
- DNS pointing to correct endpoints

The application will automatically start generating and processing transactions. You should see real-time data flowing in the dashboard within 1-2 minutes of deployment completion.
