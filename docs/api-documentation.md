# Financial Monitoring API Documentation

## Overview

The Financial Monitoring API is a RESTful web service that provides endpoints for querying financial transaction data and analytics. The API is built using ASP.NET Core 8.0 and follows modern web API best practices including authentication, rate limiting, caching, and comprehensive error handling.

**Base URLs:**
- **Local Development**: `http://localhost:5100`
- **Production**: `https://your-deployed-endpoint.com`

## API Features

### Security & Performance
- **JWT/OAuth 2.0 Authentication**: Token-based authentication with access and refresh tokens
- **Rate Limiting**: IP-based throttling to prevent abuse
- **Output Caching**: Intelligent response caching for improved performance
- **Security Headers**: Comprehensive security headers for protection
- **CORS Support**: Cross-origin resource sharing for web applications
- **Request Validation**: FluentValidation for input validation and data integrity

### Observability
- **Correlation IDs**: Request tracing across the entire system
- **Structured Logging**: Comprehensive logging with contextual information
- **Health Checks**: System health monitoring endpoints
- **Application Insights**: Distributed tracing and performance monitoring

## Middleware Pipeline

The API processes requests through a carefully ordered middleware pipeline:

```mermaid
graph LR
    A[Request] --> B[Security Headers]
    B --> C[Correlation ID]
    C --> D[Global Exception Handling]
    D --> E[CORS]
    E --> F[Rate Limiting]
    F --> G[Authentication]
    G --> H[Output Cache]
    H --> I[Controller Action]

    style B fill:#ff9999
    style F fill:#ffff99
    style G fill:#cc99ff
    style H fill:#99ff99
```

### Middleware Components

1. **Security Headers**: Adds essential security headers (X-Frame-Options, CSP, etc.)
2. **Correlation ID**: Generates unique request identifiers for tracing
3. **Global Exception Handling**: Catches and formats unhandled exceptions
4. **CORS**: Handles cross-origin requests from web applications
5. **Rate Limiting**: Enforces request rate limits per IP address
6. **Authentication**: Validates JWT Bearer tokens
7. **Output Cache**: Caches responses based on configured policies

## Authentication

The API uses JWT (JSON Web Token) Bearer authentication with OAuth 2.0 support.

### Getting Started

1. **User Authentication** (Password Grant):
   ```http
   POST /api/v2/auth/login
   Content-Type: application/json

   {
     "username": "your-username",
     "password": "your-password"
   }
   ```

2. **OAuth Client Credentials** (Machine-to-Machine):
   ```http
   POST /api/v2/oauth/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=client_credentials&client_id=your-client-id&client_secret=your-client-secret
   ```

### Using Access Tokens

Include the access token in the Authorization header:

```http
Authorization: Bearer your-access-token-here
```

**Authentication Requirements:**
- V2 endpoints require a valid JWT Bearer token
- Access tokens expire after 15 minutes (default)
- Refresh tokens can be used to obtain new access tokens (7 day expiry)
- Invalid or missing tokens return `401 Unauthorized`

## Rate Limits

The API implements IP-based rate limiting:

- **General Endpoints**: 1,000 requests per minute
- **Transaction Endpoints**: 100 requests per minute

Rate limit headers are included in responses:
- Rate limit exceeded returns `429 Too Many Requests`
- Rate limit information included in response headers

## API Versioning

The API uses URL path versioning:
- **Current Version**: `v1.0`
- **URL Format**: `/api/v{version}/endpoint`
- **Example**: `/api/v1.0/transactions`

## Response Format

### Success Response Structure
```json
{
  "success": true,
  "data": { ... },
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### Error Response Structure
```json
{
  "success": false,
  "error": {
    "title": "Validation Error",
    "detail": "Page number must be greater than 0",
    "status": 400,
    "instance": "/api/v1.0/transactions"
  },
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### HTTP Status Codes
- `200 OK`: Successful request
- `400 Bad Request`: Invalid request parameters or validation errors
- `401 Unauthorized`: Missing or invalid JWT token
- `404 Not Found`: Requested resource not found
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server-side error

## Transactions API

### Get All Transactions
Retrieves a paginated list of all transactions with optional filtering.

**Endpoint:** `GET /api/v2/transactions`

**Query Parameters:**
- `pageNumber` (integer): Page number (default: 1, min: 1)
- `pageSize` (integer): Items per page (default: 20, range: 1-100)

**Response:** `ApiResponse<PagedResult<Transaction>>`

**Caching:** 2 minutes (varies by query parameters)

**Example Request:**
```http
GET /api/v2/transactions?pageNumber=1&pageSize=20
Authorization: Bearer your-access-token-here
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "txn_123456",
        "amount": 156.78,
        "sourceAccount": {
          "accountId": "acc_789",
          "accountNumber": "****1234"
        },
        "merchantName": "Coffee Shop Downtown",
        "merchantCategory": "Food & Dining",
        "paymentMethod": "CreditCard",
        "location": {
          "city": "Seattle",
          "state": "WA",
          "country": "USA"
        },
        "timestamp": "2025-01-15T09:45:30.000Z",
        "isAnomalous": false,
        "anomalyType": null
      }
    ],
    "totalCount": 15437,
    "pageNumber": 1,
    "pageSize": 20,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### Get Transaction by ID
Retrieves a specific transaction by its unique identifier.

**Endpoint:** `GET /api/v2/transactions/{id}`

**Path Parameters:**
- `id` (string): Transaction ID (validated format)

**Response:** `ApiResponse<Transaction>`

**Caching:** 10 minutes (varies by route)

**Example Request:**
```http
GET /api/v2/transactions/txn_123456
Authorization: Bearer your-access-token-here
```

### Get Anomalous Transactions
Retrieves a paginated list of transactions flagged as anomalous.

**Endpoint:** `GET /api/v2/transactions/anomalies`

**Query Parameters:**
- `pageNumber` (integer): Page number (default: 1, min: 1)
- `pageSize` (integer): Items per page (default: 20, range: 1-100)

**Response:** `ApiResponse<PagedResult<Transaction>>`

**Caching:** 1 minute

**Example Request:**
```http
GET /api/v2/transactions/anomalies?pageNumber=1&pageSize=10
Authorization: Bearer your-access-token-here
```

### Search Transactions
Performs advanced transaction search with filtering criteria.

**Endpoint:** `POST /api/v2/transactions/search`

**Request Body:** `TransactionSearchRequest`

**Response:** `ApiResponse<PagedResult<Transaction>>`

**Caching:** 2 minutes (varies by query parameters)

**Example Request:**
```http
POST /api/v2/transactions/search
Content-Type: application/json
Authorization: Bearer your-access-token-here

{
  "pageNumber": 1,
  "pageSize": 20,
  "accountId": "acc_789",
  "merchantCategory": "Food & Dining",
  "amountMin": 10.00,
  "amountMax": 500.00,
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-01-15T23:59:59Z",
  "isAnomalous": false
}
```

## Analytics API

### Get Transaction Analytics Overview
Retrieves global transaction analytics and statistics.

**Endpoint:** `GET /api/v2/analytics/overview`

**Response:** `ApiResponse<TransactionAnalytics>`

**Caching:** Analytics cache policy

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalTransactions": 154372,
    "totalAmount": 12456789.43,
    "averageAmount": 80.73,
    "anomalousTransactions": 1247,
    "anomalyRate": 0.0081,
    "topMerchantCategory": "Food & Dining",
    "timeRange": {
      "startDate": "2025-01-01T00:00:00Z",
      "endDate": "2025-01-15T23:59:59Z"
    }
  },
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### Get Transaction Time Series
Retrieves time series data for transaction counts over a specified period.

**Endpoint:** `GET /api/v2/analytics/timeseries/transactions`

**Query Parameters:**
- `hours` (integer): Hours to look back (default: 24, range: 1-168)
- `intervalMinutes` (integer): Data point interval (default: 60, range: 1-1440)

**Response:** `ApiResponse<List<TimeSeriesDataPoint>>`

**Example Request:**
```http
GET /api/v2/analytics/timeseries/transactions?hours=48&intervalMinutes=60
Authorization: Bearer your-access-token-here
```

**Example Response:**
```json
{
  "success": true,
  "data": [
    {
      "timestamp": 1705315200000,
      "value": 347,
      "label": "2025-01-15T08:00:00Z"
    },
    {
      "timestamp": 1705318800000,
      "value": 423,
      "label": "2025-01-15T09:00:00Z"
    }
  ],
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### Get Anomaly Time Series
Retrieves time series data for anomaly counts over a specified period.

**Endpoint:** `GET /api/v2/analytics/timeseries/anomalies`

**Query Parameters:**
- `hours` (integer): Hours to look back (default: 24, range: 1-168)
- `intervalMinutes` (integer): Data point interval (default: 60, range: 1-1440)

**Response:** `ApiResponse<List<TimeSeriesDataPoint>>`

### Get Top Merchants
Retrieves analytics data for top merchants by transaction volume.

**Endpoint:** `GET /api/v2/analytics/merchants/top`

**Query Parameters:**
- `count` (integer): Number of merchants to return (default: 10, range: 1-50)

**Response:** `ApiResponse<List<MerchantAnalytics>>`

**Example Response:**
```json
{
  "success": true,
  "data": [
    {
      "merchantName": "Coffee Shop Downtown",
      "transactionCount": 1247,
      "totalAmount": 18432.56,
      "averageAmount": 14.78,
      "category": "Food & Dining"
    }
  ],
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### Get Merchant Category Analytics
Retrieves analytics data grouped by merchant category.

**Endpoint:** `GET /api/v2/analytics/merchants/categories`

**Response:** `ApiResponse<List<MerchantAnalytics>>`

## Health Endpoints

### System Health Check
Checks the health status of the API and its dependencies.

**Endpoint:** `GET /health`

**Authentication:** Not required

**Response:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "external_api": {
      "status": "Healthy",
      "duration": "00:00:00.0234567"
    }
  }
}
```

## Caching Policies

The API implements intelligent caching strategies:

### Cache Policies
- **TransactionCache**: 2 minutes, varies by query parameters
- **TransactionByIdCache**: 10 minutes, varies by route
- **AnomalousTransactionCache**: 1 minute
- **AnalyticsCache**: Analytics-specific caching policy
- **TimeSeriesCache**: Time series data caching

### Cache Headers
Responses include appropriate cache control headers:
- `Cache-Control`: Defines caching behavior
- `ETag`: Entity tags for conditional requests
- `Last-Modified`: Last modification timestamps

## Error Handling

### Validation Errors
Input validation uses FluentValidation with detailed error messages:

```json
{
  "success": false,
  "error": {
    "title": "Validation Error",
    "detail": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
      "pageNumber": ["Page number must be greater than 0"],
      "pageSize": ["Page size must be between 1 and 100"]
    }
  },
  "correlationId": "0HNF4V3LOFS99",
  "timestamp": "2025-01-15T10:30:00.000Z"
}
```

### Global Exception Handling
Unhandled exceptions are caught and formatted consistently:
- Detailed errors logged internally
- Generic error messages returned to clients
- Correlation IDs maintained for troubleshooting
