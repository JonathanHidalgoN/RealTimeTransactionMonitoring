SHELL := /bin/bash
.PHONY: deploy infra apps frontend clean status help dev bootstrap terraform app-config update-manifests k8s-setup k8s-continue build-push k8s-deploy deploy-blazor update-cors restart-workload-identity

all: deploy

deploy: infra apps frontend
	@echo ""
	@echo "Deployment Complete!"
	@echo "API: https://api.finmon-ui-azj.com"
	@echo "Dashboard: $$(cd infra && terraform output -raw static_web_app_default_hostname 2>/dev/null || echo 'Check Azure portal')"
	@echo ""

infra:
	@$(MAKE) bootstrap
	@echo ""
	@echo "MANUAL STEP REQUIRED:"
	@echo "Run the following commands in your shell to deploy the core infrastructure:"
	@echo "1. source .terraform.env && source .env"
	@echo "2. cd infra"
	@echo "3. terraform init -upgrade"
	@echo "4. terraform import azurerm_resource_group.rg \"/subscriptions/$$(az account show --query id -o tsv)/resourceGroups/$$RESOURCE_GROUP_NAME\" || echo 'Skipping import...'"
	@echo "5. terraform plan -out=tfplan"
	@echo "6. terraform apply tfplan"
	@echo ""
	@echo "After the commands complete successfully, run: make terraform-continue"
	@echo ""

apps: 
	@$(MAKE) build-push
	@$(MAKE) k8s-setup
	@echo ""
	@echo "DEPLOYMENT PAUSED - Manual step required"
	@echo "1. Update your DNS A record as shown above"
	@echo "2. Verify DNS propagation (may take 2-5 minutes):"
	@echo "   nslookup api.finmon-ui-azj.com"
	@echo "   (Should return the LoadBalancer IP above)"
	@echo "3. Run: make apps-continue"
	@echo ""

frontend:
	@$(MAKE) deploy-blazor
	@$(MAKE) update-cors
	@echo "Frontend deployment complete"

bootstrap:
	@echo "Setting up Azure infrastructure foundation..."
	./scripts/bootstrap.sh

#terraform:
#	@echo "Deploying Azure resources with Terraform..."
#	@if [ ! -f .terraform.env ]; then \
#		echo "Error: .terraform.env not found. Run 'make bootstrap' first."; \
#		exit 1; \
#	fi
#	@echo "Initializing Terraform..."
#	@bash -c "source .terraform.env && cd infra && terraform init -upgrade"
#	@echo "Importing existing resource group..."
#	@bash -c "source .terraform.env && source .env && export RESOURCE_GROUP_NAME && cd infra && terraform import azurerm_resource_group.rg \"/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP_NAME-rg\"" || echo "Resource group already imported or doesn't exist"
#	@echo "Applying Terraform configuration..."
#	@bash -c "source .terraform.env && cd infra && terraform plan -out=tfplan && terraform apply tfplan"

terraform:
	# This target is intentionally left blank.
	# The 'infra' target provides instructions for manual terraform deployment.

terraform-continue:
	@echo "Continuing infrastructure deployment..."
	@$(MAKE) app-config
	@echo "Infrastructure deployment complete"


app-config:
	@echo "Setting up application configuration..."
	@bash -c "source .terraform.env && ./scripts/setup_app_config.sh"

update-manifests:
	@echo "Updating Kubernetes manifests with ACR name and client ID..."
	@bash -c "source .terraform.env && ./scripts/update-k8s-manifests.sh"

k8s-setup:
	@echo "Setting up Kubernetes infrastructure..."
	@echo "Connecting to AKS cluster..."
	@source .env && source .terraform.env && \
	AKS_NAME=$$(cd infra && terraform output -raw aks_cluster_name) && \
	az aks get-credentials --resource-group "$$RESOURCE_GROUP_NAME" --name "$$AKS_NAME" --overwrite-existing
	@echo "Installing NGINX Ingress Controller..."
	./scripts/setup-ingress-controller.sh
	@echo ""
	@echo "MANUAL STEP REQUIRED:"
	@echo "Update your DNS A record:"
	@echo "Domain: api.finmon-ui-azj.com"
	@echo "Points to: $$(kubectl get service ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo 'LoadBalancer IP pending')"
	@echo ""
	@echo "After updating DNS, run: make apps-continue"
	@echo ""

k8s-continue:
	@echo "Installing cert-manager for SSL..."
	./scripts/install-cert-manager.sh

apps-continue:
	@echo "Continuing application deployment..."
	@$(MAKE) k8s-deploy
	@$(MAKE) k8s-continue
	@echo "Application deployment complete"

build-push:
	@echo "Building and pushing container images..."
	@bash -c "source .terraform.env && ./scripts/build-and-push-acr.sh"

k8s-deploy:
	@echo "Updating Kubernetes manifests with correct ACR name..."
	@$(MAKE) update-manifests
	@echo "Deploying applications to Kubernetes..."
	kubectl apply -k k8s-manifest/
	@echo "Updating ConfigMap with live infrastructure values..."
	@bash -c "source .terraform.env && ./scripts/update-configmap.sh"
	@echo "Ensuring Azure Workload Identity is functioning..."
	@$(MAKE) restart-workload-identity
	@echo "Waiting for pods to be ready..."
	@kubectl wait --for=condition=Ready pod -l app=financial-api -n finmon-app --timeout=300s || echo "API pods may still be starting"
	@kubectl wait --for=condition=Ready pod -l app=transaction-processor -n finmon-app --timeout=300s || echo "Processor pods may still be starting"
	@kubectl wait --for=condition=Ready pod -l app=transaction-simulator -n finmon-app --timeout=300s || echo "Simulator pods may still be starting"

deploy-blazor:
	@echo "Deploying Blazor frontend to Static Web Apps..."
	@bash -c "source .terraform.env && ./scripts/deploy-blazor-static-app.sh"

update-cors:
	@echo "Updating CORS configuration..."
	@bash -c "source .terraform.env && ./scripts/update-cors.sh"

restart-workload-identity:
	@echo "Restarting Azure Workload Identity webhook..."
	./scripts/restart-workload-identity.sh

dev:
	@echo "Starting development environment..."
	./scripts/cost-management.sh work

clean:
	@echo "Stopping AKS cluster to save costs..."
	./scripts/cost-management.sh stop

start:
	@echo "Starting AKS cluster..."
	./scripts/cost-management.sh start

demo:
	@echo "Scaling for demo (2 nodes)..."
	./scripts/cost-management.sh demo

status:
	@echo "Deployment Status:"
	@echo "=================="
	@echo "Pods:"
	@kubectl get pods -n finmon-app 2>/dev/null || echo "Not connected to cluster"
	@echo ""
	@echo "Ingress:"
	@kubectl get ingress -n finmon-app 2>/dev/null || echo "Not connected to cluster"
	@echo ""
	@echo "LoadBalancer IP:"
	@kubectl get service ingress-nginx-controller -n ingress-nginx -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "Not available"

logs:
	@echo "Application Logs:"
	@echo "API Logs:"
	@kubectl logs -l app=api -n finmon-app --tail=20
	@echo ""
	@echo "Processor Logs:"
	@kubectl logs -l app=processor -n finmon-app --tail=20
	@echo ""
	@echo "Simulator Logs:"
	@kubectl logs -l app=simulator -n finmon-app --tail=20

test:
	@echo "Testing deployment..."
	@echo "Testing API health:"
	@curl -k -f https://api.finmon-ui-azj.com/healthz || echo "API health check failed"
	@echo ""
	@echo "Testing transactions endpoint:"
	@curl -k -f https://api.finmon-ui-azj.com/api/transactions || echo "Transactions endpoint failed"

test-suite:
	@echo "Running test suite..."
	./scripts/run-tests.sh

help:
	@echo "Real-Time Financial Monitoring - Deployment Commands"
	@echo "===================================================="
	@echo ""
	@echo "Main Commands:"
	@echo "  make deploy       - Complete deployment from scratch"
	@echo "  make infra        - Infrastructure only (bootstrap + terraform + app-config)"
	@echo "  make apps         - Applications only (k8s-setup + build-push + k8s-deploy)"
	@echo "  make frontend     - Frontend only (deploy-blazor + update-cors)"
	@echo ""
	@echo "Individual Steps:"
	@echo "  make bootstrap    - Setup Azure foundation"
	@echo "  make terraform    - Deploy Azure infrastructure"
	@echo "  make app-config   - Setup application configuration"
	@echo "  make k8s-setup    - Setup Kubernetes (ingress + connect)"
	@echo "  make k8s-continue - Continue after DNS update (SSL)"
	@echo "  make build-push   - Build and push containers"
	@echo "  make k8s-deploy   - Deploy to Kubernetes"
	@echo "  make deploy-blazor - Deploy Blazor frontend"
	@echo "  make update-cors  - Update CORS configuration"
	@echo "  make restart-workload-identity - Fix Workload Identity authentication issues"
	@echo ""
	@echo "Development:"
	@echo "  make dev          - Start development environment"
	@echo "  make test         - Test deployment endpoints"
	@echo "  make test-suite   - Run full test suite"
	@echo ""
	@echo "Cost Management:"
	@echo "  make clean        - Stop cluster (save costs)"
	@echo "  make start        - Start cluster"
	@echo "  make demo         - Scale for demo (2 nodes)"
	@echo ""
	@echo "Monitoring:"
	@echo "  make status       - Show deployment status"
	@echo "  make logs         - Show application logs"
