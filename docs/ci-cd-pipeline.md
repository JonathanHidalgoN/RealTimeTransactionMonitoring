# CI/CD Pipeline Documentation

## Overview

The Real-Time Financial Monitoring System implements a comprehensive CI/CD pipeline using GitHub Actions that automatically builds, tests, and deploys the entire microservices stack to Azure. The pipeline ensures code quality through multiple testing phases.

## Pipeline Architecture

The automated deployment pipeline handles testing, building, and deployment of all components:

```mermaid
graph TD
    DEV[Developer] -- Git Push --> REPO[GitHub Repository]
    REPO -- Triggers --> ACTIONS[GitHub Actions]

    subgraph "Testing Phase"
        ACTIONS --> UNIT[Unit Tests]
        ACTIONS --> INTEGRATION[Integration Tests]
        ACTIONS --> LOAD[Load Tests]
    end

    LOAD --> BUILD

    subgraph "Build & Push Phase"
        BUILD[Build 4 Docker Images]
        BUILD --> API_IMG[API Image]
        BUILD --> PROC_IMG[Processor Image]
        BUILD --> SIM_IMG[Simulator Image]
        BUILD --> WEB_IMG[WebApp Image]

        API_IMG --> ACR[Azure Container Registry]
        PROC_IMG --> ACR
        SIM_IMG --> ACR
        WEB_IMG --> ACR
    end

    subgraph "Deploy Phase"
        ACR --> K8S_UPDATE[Update K8s Manifests]
        K8S_UPDATE --> AKS[Deploy to AKS]
        WEB_IMG --> SWA[Deploy to Static Web Apps]
    end
```

