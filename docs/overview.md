# Real-Time Financial Monitoring System

## Introduction

The Real-Time Financial Monitoring System is a comprehensive, microservices-based platform designed to simulate, process, and monitor financial transactions in real-time. The system provides fraud detection, anomaly detection, and analytics capabilities for financial transaction data.

This project demonstrates modern software engineering practices including:
- **Microservices Architecture**: Independent, scalable services
- **Event-Driven Design**: Asynchronous message processing
- **Cloud-Native Deployment**: Azure and local development support
- **Real-Time Processing**: Stream processing with Kafka/EventHubs
- **Multiple Database Support**: MongoDB and CosmosDB adapters
- **Comprehensive Testing**: Unit and integration testing

## System Architecture

The system follows a microservices architecture with clear separation of concerns:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Transaction    │ -> │     Message      │ -> │  Transaction    │ -> Is anomalous
│   Simulator     │    │  Queue (Kafka)   │    │   Processor     │         │
└─────────────────┘    └──────────────────┘    └─────────────────┘         │
                                                         │                 │
                                                         v                 │
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐         │
│   Web Dashboard │ <- │   REST API       │ <- │    Database     │         │
│   (Blazor)      │    │   (ASP.NET)      │    │ (MongoDB/Cosmos)│         │
└─────────────────┘    └──────────────────┘    └─────────────────┘         │
                                                                           │
                      ┌──────────────────┐    ┌─────────────────┐          │
                      │  Notification    │ <- │  Anomaly queue  │ <--------│
                      │     service      │    │(EventHubs/kafka)│
                      └──────────────────┘    └─────────────────┘
```

## Project Structure

The source code is organized in the `src/` directory with the following components:

### Core Projects

#### **FinancialMonitoring.Abstractions/**
- **Purpose**: Contains all interfaces and contracts used across the system
- **Key Components**:
  - `Messaging/`: Message producer/consumer interfaces (`IMessageProducer`, `IMessageConsumer`)
  - `Persistence/`: Repository patterns (`ITransactionRepository`, `IAnalyticsRepository`)
  - `Caching/`: Redis caching abstractions (`IRedisCacheService`)
  - `ITransactionAnomalyDetector`: Interface for anomaly detection algorithms
  - `ITransactionGenerator`: Interface for transaction generation strategies

#### **FinancialMonitoring.Models/**
- **Purpose**: Shared data models, DTOs, and configuration classes
- **Key Components**:
  - `Transaction.cs`: Core transaction entity
  - `Account.cs`, `Location.cs`: Supporting entities
  - `Analytics/`: Analytics-specific models (`MerchantAnalytics`, `TimeSeriesDataPoint`)
  - Configuration classes: `KafkaSettings`, `MongoDbSettings`, `EventHubsSettings`
  - `ApiResponse.cs`: Standardized API response wrapper
  - `PagedResult.cs`: Pagination support

#### **FinancialMonitoring.Api/**
- **Purpose**: REST API service for querying transactions and analytics
- **Key Components**:
  - `Controllers/`: REST endpoints (`TransactionsController`, `AnalyticsController`)
  - `Authentication/`: API key authentication handlers
  - `Middleware/`: Security headers, correlation ID, global exception handling
  - `Validation/`: Request validation using FluentValidation
  - `HealthChecks/`: Health monitoring endpoints
  - `Swagger/`: API documentation configuration

### Microservices

#### **TransactionSimulator/**
- **Purpose**: Generates realistic financial transaction data
- **Key Components**:
  - `Simulator.cs`: Main background service for transaction generation
  - `Generation/`: Transaction generation logic (`TransactionGenerator`, `SimpleTransactionGenerator`)
  - `Data/`: Realistic data sets (`LocationData`, `MerchantData`)
  - `Messaging/`: Message producers for Kafka/EventHubs integration
- **Behavior**: Continuously generates and publishes transactions to message queues

#### **TransactionProcessor/**
- **Purpose**: Consumes and processes transactions for anomaly/fraud detection
- **Key Components**:
  - `Worker.cs`: Background service that consumes messages
  - `AnomalyDetection/`: Detection algorithms (`AnomalyDetector`, `StatefulAnomalyDetector`)
  - `Messaging/`: Message consumers and anomaly event publishers
  - `Caching/`: Redis integration for stateful processing
- **Behavior**: Processes incoming transactions, detects anomalies, stores results

#### **FinancialMonitoring.WebApp/**
- **Purpose**: Blazor Server dashboard for monitoring and analytics
- **Key Components**:
  - `Pages/`: Dashboard pages (`Dashboard.razor`, `Analytics.razor`)
  - `Services/`: API client for backend communication
  - `Layout/`: Shared UI components and navigation
- **Behavior**: Provides real-time monitoring interface for transactions and analytics

## Technology Stack

- **Runtime**: .NET 8.0
- **Web Framework**: ASP.NET Core (API), Blazor Server (Web UI)
- **Message Queues**: Apache Kafka (local), Azure EventHubs (cloud)
- **Databases**: MongoDB (local), Azure CosmosDB (cloud)
- **Caching**: Redis (for stateful anomaly detection)
- **Containerization**: Docker & Docker Compose
- **Cloud Platform**: Microsoft Azure
- **Orchestration**: Kubernetes (Azure AKS)

## Key Features

### Real-Time Processing
- Asynchronous message processing with Kafka/EventHubs
- Stream processing for continuous transaction analysis
- Real-time anomaly detection and alerting

### Multi-Environment Support
- **Local Development**: Docker Compose with MongoDB and Kafka
- **Cloud Deployment**: Azure with CosmosDB and EventHubs
- **Testing**: Isolated test containers with dedicated databases

### Scalability & Reliability
- Microservices can be scaled independently
- Health checks and monitoring
- Graceful error handling and circuit breaker patterns
- Comprehensive logging and correlation IDs

### Security
- API key authentication
- Rate limiting
- Security headers middleware
- Input validation and sanitization
