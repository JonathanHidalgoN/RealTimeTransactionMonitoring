# API Reference

Base URL: `http://localhost:5000` (local) or your deployed endpoint

## Authentication

Currently using API key authentication. Include in header:
```
X-API-Key: your-api-key-here
```

## Endpoints

### Transactions

**GET /api/transactions**
- Get all transactions with optional filtering
- Query params: `userId`, `startDate`, `endDate`, `limit`

**GET /api/transactions/{id}**
- Get specific transaction by ID

**POST /api/transactions**
- Create new transaction
- Body: `{ "amount": 100.50, "userId": "user123", "category": "food" }`

### Alerts

**GET /api/alerts**
- Get alerts with optional filtering
- Query params: `severity`, `startDate`, `endDate`

**GET /api/alerts/{id}**
- Get specific alert by ID

### Health

**GET /health**
- System health check
- Returns 200 OK if all dependencies are healthy

## Response Format

**Success Response:**
```json
{
  "success": true,
  "data": { ... },
  "timestamp": "2024-01-01T12:00:00Z"
}
```

**Error Response:**
```json
{
  "success": false,
  "error": "Error message",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

## Rate Limits

- 1000 requests per minute per API key
- 429 status code when exceeded