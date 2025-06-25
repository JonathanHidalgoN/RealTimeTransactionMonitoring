# Real-Time Financial Transactions Monitoring System

This project is a complete, cloud-native application designed to ingest, process, and monitor financial transaction data in real-time. It leverages a modern microservices-style architecture deployed on Azure Kubernetes Service (AKS) and a Blazor WebAssembly front-end hosted on Azure Static Web Apps.

The primary goal is to identify and flag anomalous transactions, provide a queryable API, send notifications, and visualize the entire process on an interactive web dashboard. The entire infrastructure is managed as code using Terraform, with a full CI/CD pipeline for automated builds and deployments.

## Architecture

The system follows an event-driven architecture, ensuring scalability and resilience.

```mermaid
graph TD
    subgraph "User & CI/CD"
        U[User] --> SWA[Azure Static Web Apps];
        A[Developer] -- Git Push --> B[GitHub Repository];
        B -- Triggers --> C[GitHub Actions CI/CD];
        C -- Deploys UI --> SWA;
        C -- Deploys Backend --> F[Azure Kubernetes Service];
    end

    subgraph "Azure Runtime Infrastructure"
        SWA -- Calls API --> L[API Service on AKS];
        G[Transaction Simulator on AKS] -- Produces Events --> H["Azure Event Hubs (transactions)"];
        H -- Streams Data --> I[Transaction Processor on AKS];

        I -- Reads/Writes Stats --> Q[Azure Cache for Redis];

        I -- Publishes Anomaly --> N["Azure Event Hubs (anomalies)"];
        N -- Triggers --> O[Azure Logic App];
        O -- Sends Email --> P([Email Notification]);
        I -- Checks for Anomalies & Stores Data --> J[Azure Cosmos DB];
        I -- Reads Secrets --> K[Azure Key Vault];
        L -- Queries Data --> J;
        L -- Reads Secrets --> K;
        E[Azure Container Registry] -- Provides Images --> F;
    end

    subgraph "Observability"
        G --> M[Application Insights];
        I --> M;
        L --> M;
    end
```

## Key Features

* **Interactive Web UI:** A dashboard built with **Blazor WebAssembly** and hosted on **Azure Static Web Apps** provides a live view of transactions, KPIs, and charts.
* **Real-Time Event Ingestion:** Uses Azure Event Hubs to handle high-throughput data streams.
* **Asynchronous Processing:** A .NET Worker Service consumes events and processes them independently.
* **Stateful Anomaly Detection:** An extensible system for flagging suspicious transactions. Uses **Azure Cache for Redis** to maintain real-time account statistics for more intelligent rule-based detection.
* **Serverless Notifications:** Uses **Azure Logic Apps** to send email alerts when an anomaly is detected.
* **Scalable NoSQL Persistence:** Uses Azure Cosmos DB (SQL API, Free Tier) for efficient storage.
* **Cloud-Native Deployment:** The entire application stack is containerized with Docker and orchestrated by **Azure Kubernetes Service (AKS)** with health probes and resource limits.
* **Automated Scaling:** The API autoscales using the Horizontal Pod Autoscaler (HPA), and the cluster itself scales with the Cluster Autoscaler.
* **Infrastructure as Code (IaC):** All Azure resources are defined and managed declaratively using **Terraform**.
* **End-to-End CI/CD:** A **GitHub Actions** workflow automates the entire process from commit to cloud deployment for both the backend and front-end.
* **Secure Configuration & Identity:**
    * Secrets are stored securely in **Azure Key Vault**.
    * The API is secured using **API Key authentication**.
    * Services running in AKS use **Azure AD Workload Identity** for a modern, secure, and passwordless authentication to Key Vault.
* **Centralized Observability:** All services are instrumented with **Application Insights** for distributed tracing, logging, and performance monitoring.

## Technology Stack

* **Languages & Frameworks:** C# 12, .NET 8, ASP.NET Core (Web API), Worker Service, Blazor WebAssembly, xUnit
* **Azure Cloud Services:**
    * Azure Kubernetes Service (AKS)
    * Azure Container Registry (ACR)
    * Azure Static Web Apps
    * Azure Cosmos DB (SQL API, Free Tier)
    * Azure Event Hubs (Basic Tier)
    * Azure Cache for Redis
    * Azure Logic Apps
    * Azure Key Vault
    * Azure Active Directory (Workload Identity)
    * Application Insights & Log Analytics Workspace
    * Azure Storage (for Terraform remote state)
* **Tools & Concepts:** Docker, Kubernetes (Manifests with Kustomize), Terraform, GitHub Actions, Git, REST API, Dependency Injection

## Project Structure

```
.
├── .github/workflows/      # GitHub Actions CI/CD pipeline definitions
├── infra/                  # Terraform files for all Azure infrastructure
├── k8s-manifests/          # Kubernetes manifest files (Deployments, Services, etc.)
├── setup/                  # Contains bootstrap and app configuration scripts
│   ├── bootstrap.sh
│   └── setup_app_config.sh
├── src/                    # .NET source code
│   ├── FinancialMonitoring.Abstractions/
│   ├── FinancialMonitoring.Api/
│   ├── FinancialMonitoring.Models/
│   ├── FinancialMonitoring.WebApp/ # Blazor WASM UI Project
│   ├── TransactionProcessor/
│   └── TransactionSimulator/
└── tests/                  # xUnit test projects
    ├── FinancialMonitoring.Api.Tests/
    └── FinancialMonitoring.Models.Tests/
```

## Getting Started: Cloud Deployment Guide

This guide outlines the end-to-end process to provision the Azure infrastructure and deploy the application from a fresh repository clone.

### Prerequisites

* An active Azure Subscription.
* [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
* [Terraform CLI](https://www.terraform.io/downloads.html)
* [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)
* [jq](https://stedolan.github.io/jq/download/) (a command-line JSON processor)
* Docker Desktop (or Docker Engine)

### Deployment Steps

The setup is largely automated via scripts and the CI/CD pipeline.

1.  **Bootstrap Infrastructure Prerequisites:** Run the `./setup/bootstrap.sh` script first. It creates the foundational resources (Resource Group, Terraform State Storage) and the primary Service Principal for Terraform. It will output instructions for the next steps.

2.  **Provision Main Infrastructure:** Following the instructions from the bootstrap script, you will run `terraform apply`. This creates the AKS cluster, ACR, Cosmos DB, Event Hubs, Redis Cache, and the Static Web App.

3.  **Configure Application Identity & Secrets:** The `./setup/setup_app_config.sh` script automates creating the application's identity and populating Key Vault.

4.  **Trigger the CI/CD Pipeline:** Commit and push all your code to the `main` branch of your GitHub repository. The GitHub Actions workflow will automatically:
    * Build and test your .NET solution.
    * Build all production Docker images and push them to your Azure Container Registry.
    * Deploy your backend services to Azure Kubernetes Service.
    * Deploy your Blazor UI to Azure Static Web Apps.

5.  **Access Your Application:** Once the pipeline succeeds, find the URL of your deployed Static Web App (from the `terraform output` or the Azure Portal) and navigate to it in your browser.

## Future Enhancements

* **Advanced API Security:** Enhance the current API Key authentication with a standard OAuth 2.0 / JWT-based flow for user-level access.
* **Advanced CI/CD:** Implement multi-stage pipelines for deploying to `staging` and `production` environments with manual approvals.
* **Deepen Observability:** Create custom Azure Dashboards to visualize system health and performance metrics from Application Insights.
