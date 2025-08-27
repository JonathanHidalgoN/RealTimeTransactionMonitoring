# Transaction Simulator

## Overview

The Transaction Simulator is data generation service that creates financial transaction data. It generates transactions with behavioral patterns based on real-world characteristics and user personas.

## Key Features

### Transaction Generation

- **User Profile-Based Behavior**: Creates diverse user personas with distinct spending patterns
- **Temporal Intelligence**: Realistic time-based transaction patterns
- **Geographic Distribution**: Authentic US location data with travel modeling
- **Merchant Ecosystem**: Real merchant names across 12+ categories
- **Payment Method Intelligence**: Context-aware payment method selection

### Behavioral Modeling

#### User Types and Characteristics
- **Students**: Late-night activity, education expenses, entertainment focus
- **Young Professionals**: Travel spending, entertainment, higher restaurant usage
- **Family Persons**: Healthcare, education, grocery emphasis
- **Retirees**: Daytime activity, travel, conservative spending
- **High Net Worth**: Luxury travel, high-value retail, premium services
- **Small Business**: Early/late activity, variable amounts, business categories
- **Freelancers**: Irregular patterns, online services, flexible schedules

#### Spending Patterns
Each user type has customized spending patterns with:
- **Monthly Frequency**: How often they transact in each category
- **Average Amount**: Typical spending amount with standard deviation
- **Preferred Hours**: Time-of-day preferences for different activities
- **Weekend Probability**: Different behavior on weekends vs weekdays

### Realistic Transaction Attributes

#### Amount Generation
- **Normal Distribution**: Uses Gaussian distribution around user patterns
- **Category Bounds**: Realistic ranges per merchant category
  - Grocery: $5-300
  - Restaurant: $8-150
  - Travel: $100-5,000
  - Healthcare: $25-5,000
  - ATM: $20-500

#### Payment Method Logic
- **Large Amounts (>$1,000)**: Check or ACH transfer
- **ATM Transactions**: Cash withdrawal
- **Online Services**: Digital wallet or credit card
- **General Purchases**: Debit card (60%) or credit card (40%)

#### Geographic Intelligence
- **Home Location**: Each user has a base location
- **Travel Probability**: User-type specific travel likelihood
- **Nearby Transactions**: Most transactions near home location
- **Time Zone Awareness**: Adjusts transaction times for user's location

## Technical Implementation

### Core Components

#### 1. Simulator Service (`Simulator.cs`)
- **Background Service**: Runs continuously as a hosted service
- **Generation Loop**: Creates transactions every 20 seconds
- **Message Production**: Sends transactions to Kafka/Event Hubs
- **Health Monitoring**: Updates liveness probe timestamps

#### 2. Transaction Generators

**TransactionGenerator (Realistic)**
- **User Profile System**: 100 diverse user profiles with realistic characteristics
- **Temporal Modeling**: Time-weighted transaction generation
- **Behavioral Consistency**: Users maintain spending patterns over time
- **Anomaly Injection**: Controlled insertion of suspicious patterns

**SimpleTransactionGenerator (Testing)**
- **Basic Patterns**: Simple random transactions for testing
- **Fixed Parameters**: Consistent data for unit tests
- **Lightweight**: Minimal resource usage

## Monitoring and Observability

### Health Checks
- **Liveness Probe**: Updates `/tmp/healthy` file every 20 seconds
- **Message Production**: Logs successful/failed message production
- **Error Handling**: Comprehensive exception logging

### Logging
```csharp
// Structured logging with context
_logger.LogInformation("[{Timestamp:HH:mm:ss}] Produced realistic transaction {Counter}: {AccountId} -> {MerchantName} (${Amount}) in {Location}",
    DateTime.Now, transactionCounter, transaction.SourceAccount.AccountId,
    transaction.MerchantName, transaction.Amount, transaction.Location.City);
```

