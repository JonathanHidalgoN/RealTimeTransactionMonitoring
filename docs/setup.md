# Local Development Setup

## Prerequisites
- Docker and Docker Compose
- .NET 8 SDK
- Git

## Quick Start

1. **Clone and setup:**
   ```bash
   git clone <repo-url>
   cd RealTimeFinancialMonitoring
   ```

2. **Start infrastructure:**
   ```bash
   docker-compose up -d
   ```

3. **Run the API:**
   ```bash
   cd src/FinancialMonitoring.Api
   dotnet run
   ```

4. **Run the WebApp:**
   ```bash
   cd src/FinancialMonitoring.WebApp
   dotnet run
   ```

## What's Running

- **MongoDB**: `localhost:27017` - Main database
- **Kafka**: `localhost:9092` - Event streaming
- **Redis**: `localhost:6379` - Caching
- **API**: `localhost:5000` - REST API
- **WebApp**: `localhost:5001` - Frontend

## Common Issues

**MongoDB connection fails**: Make sure Docker is running and containers are healthy
**Kafka errors**: Wait 30 seconds after `docker-compose up` for Kafka to be ready
**Port conflicts**: Check if anything else is using ports 5000, 5001, 9092, 27017, 6379