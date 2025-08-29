# Scripts Documentation

This document provides an overview of all automation scripts in the project, organized by category. These scripts streamline development, deployment, and operations for the Real-Time Financial Monitoring system.

## Cloud Deployment Scripts (`scripts/cloudDeployment/`)

### bootstrap.sh
Sets up the foundational Azure infrastructure for the financial monitoring system. Creates resource groups, storage accounts, and initializes Terraform state management. This is the first script to run when deploying to a new Azure subscription, establishing the baseline infrastructure required for all other deployments.

### build-and-push-acr.sh
Builds Docker images for all application components and pushes them to Azure Container Registry (ACR). Handles the API, transaction processor, transaction simulator, and web application images. This script is essential for cloud deployments where the Kubernetes cluster needs to pull images from the container registry rather than using local builds.

### setup_app_config.sh
Configures Azure Application Configuration and Key Vault integration for the deployed environment. Sets up centralized configuration management, secret storage, and environment-specific settings. This script ensures that application components can securely access configuration data and secrets in the cloud environment.

### install-cert-manager.sh
Installs and configures cert-manager for automatic SSL certificate management in Kubernetes. Sets up Let's Encrypt integration to automatically provision and renew HTTPS certificates for the application endpoints. This provides secure HTTPS access for production deployments.

### deploy-blazor-static-app.sh
Deploys the Blazor WebAssembly frontend application to Azure Static Web Apps. Handles the build process and deployment of the client-side application, providing global CDN distribution and optimized static asset serving for the user interface.

## General Utilities (`scripts/generalUtils/`)

### run-tests.sh
Comprehensive testing orchestration script that builds and runs the complete test environment using Docker Compose. Executes unit tests, integration tests, and load tests in isolated containers. This is the primary testing command for ensuring code quality and system reliability across all components.

### cost-management.sh
Manages Azure Kubernetes Service (AKS) cluster resources to optimize costs. Provides commands to stop/start the cluster and scale node pools to zero during non-development periods. Essential for controlling cloud costs in development and staging environments.

### update-k8s-manifests.sh
Updates Kubernetes manifest files with current image tags and configurations. Ensures that manifest files reference the correct container image versions and environment-specific configurations before deployment. Maintains consistency between local development and cloud deployment configurations.

### restart-workload-identity.sh
Restarts Azure Workload Identity webhook components to resolve authentication issues. Useful for troubleshooting cases where Azure service authentication fails after cluster operations or configuration changes. Also restarts application pods to refresh workload identity tokens.

### switch-architecture.sh
Switches between different cloud deployment architectures (AKS vs Container Apps) by updating Terraform configuration. Allows choosing between full Kubernetes deployment for production features or Azure Container Apps for cost optimization. Provides architecture comparison and guides users through the switching process.

## Local Development Automation (`scripts/localDevelopmentAutomation/`)

### create-k3d-cluster.sh
Creates a local k3d Kubernetes cluster for development and testing. Sets up a lightweight Kubernetes environment with configurable port mapping for accessing local services. This provides a local equivalent of the production Kubernetes environment for development work.

### build-images-local-development.sh
Builds Docker images locally using development Dockerfiles optimized for faster builds and debugging. Creates tagged images for the API, transaction processor, transaction simulator, and web application components. These images are optimized for local development with faster build times and debugging capabilities.

### import-k3d-images.sh
Imports locally built Docker images into the k3d cluster registry. This bridges local Docker builds with the local Kubernetes cluster, allowing the cluster to use locally built images without needing to push to a remote registry during development.

### apply-k8s-individual.sh
Applies Kubernetes manifests to the local k3d cluster using Kustomize overlays. Uses local-specific configuration overlays to deploy all application components with appropriate local development settings. This deploys the complete application stack to the local Kubernetes environment.

## Usage Patterns

### Full Local Development Workflow(Experimental, my computer can't run Kubernetes locally ): )
1. `create-k3d-cluster.sh` - Set up local cluster
2. `build-images-local-development.sh` - Build local images
3. `import-k3d-images.sh` - Import images to cluster
4. `apply-k8s-individual.sh` - Deploy applications

### Cloud Deployment Workflow
1. `bootstrap.sh` - Initialize Azure infrastructure
2. `switch-architecture.sh [aks|containerapp] apply` - Choose deployment architecture
3. `setup_app_config.sh` - Configure application settings
4. `build-and-push-acr.sh` - Build and push container images
5. `install-cert-manager.sh` - Set up SSL certificates (AKS only)
6. `deploy-blazor-static-app.sh` - Deploy frontend application

### Testing and Maintenance
- `run-tests.sh` - Execute comprehensive test suite
- `cost-management.sh stop` - Stop AKS cluster to save costs
- `restart-workload-identity.sh` - Fix authentication issues
- `update-k8s-manifests.sh` - Update deployment configurations
