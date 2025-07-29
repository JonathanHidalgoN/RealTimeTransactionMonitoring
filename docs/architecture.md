# System Architecture

## Services Overview

### FinancialMonitoring.Api
- **Purpose**: REST API for transaction management and queries
- **Port**: 5000
- **Database**: MongoDB
- **Key endpoints**: `/api/transactions`, `/api/alerts`

### FinancialMonitoring.WebApp
- **Purpose**: Blazor frontend for dashboards and monitoring
- **Port**: 5001
- **Connects to**: API service

### TransactionProcessor
- **Purpose**: Processes incoming transactions and detects anomalies
- **Input**: Kafka events
- **Output**: Alerts to database, processed transactions

### TransactionSimulator
- **Purpose**: Generates synthetic transaction data for testing
- **Output**: Sends events to Kafka

## Data Flow

1. **Transaction Creation**: Simulator → Kafka → Processor → MongoDB
2. **Anomaly Detection**: Processor analyzes patterns → Creates alerts
3. **API Queries**: WebApp → API → MongoDB → Results
4. **Real-time Updates**: Calling API from web browser

## Database Schema

**Transactions Collection:**
- `id`, `amount`, `timestamp`, `userId`, `location`, `category`

**Alerts Collection:**
- `id`, `transactionId`, `alertType`, `severity`, `timestamp`

## Key Technologies

- **.NET 8**: Main framework
- **MongoDB**: Primary database (migrating from CosmosDB)
- **Kafka**: Event streaming
- **Redis**: Caching layer
- **Blazor**: Frontend framework
- **Docker**: Local development environment
