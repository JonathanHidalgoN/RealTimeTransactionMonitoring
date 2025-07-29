# Deployment Guide

## Azure Deployment

### Prerequisites
- Azure subscription
- Azure CLI
- Terraform CLI
- kubectl
- jq (JSON processor)
- Docker Desktop

### Quick Deploy (Complete Deployment)

For a complete deployment from scratch:
```bash
make deploy
```

This orchestrates the entire 3-phase deployment process automatically.

### Phase-by-Phase Deployment

#### Phase 1: Infrastructure (`make infra`)
1. **Bootstrap setup:**
   ```bash
   make bootstrap  # Sets up Azure foundation
   ```

2. **Terraform deployment:**
   ```bash
   source .terraform.env && source .env
   cd infra
   terraform init -upgrade
   terraform import azurerm_resource_group.rg "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP_NAME" || echo 'Skipping import...'
   terraform plan -out=tfplan
   terraform apply tfplan
   ```

3. **Configure application:**
   ```bash
   make terraform-continue  # Populates Key Vault with secrets
   ```

#### Phase 2: Applications (`make apps`)
1. **Build and deploy:**
   ```bash
   make build-push     # Build images and push to ACR
   make k8s-setup      # Setup Kubernetes + Ingress
   ```

2. **Manual DNS step:** Update DNS A record for `api.finmon-ui-azj.com` to LoadBalancer IP

3. **Continue deployment:**
   ```bash
   make apps-continue  # Install cert-manager and complete K8s deployment
   ```

#### Phase 3: Frontend (`make frontend`)
```bash
make deploy-blazor  # Deploy Blazor to Static Web Apps
make update-cors    # Configure CORS
```

### Management Commands

**Cost Management:**
```bash
make clean    # Stop cluster to save costs
make start    # Start cluster
make demo     # Scale for demo (2 nodes)
```

**Monitoring:**
```bash
make status   # Show deployment status
make logs     # Show application logs
make test     # Test deployment endpoints
```

### Individual Commands

For troubleshooting or granular control:
```bash
make bootstrap          # Setup Azure foundation only
make terraform-continue # Configure secrets only
make build-push        # Build and push images only
make k8s-setup         # Setup Kubernetes only
make deploy-blazor     # Deploy frontend only
make update-cors       # Update CORS only
```

### Architecture Notes

- **AKS Cluster**: Runs API, TransactionProcessor, TransactionSimulator
- **Static Web Apps**: Hosts Blazor WebAssembly frontend
- **Cosmos DB**: Primary data storage
- **Event Hubs**: Transaction and anomaly event streaming
- **Key Vault**: Secure configuration and secrets
- **Application Insights**: Monitoring and telemetry