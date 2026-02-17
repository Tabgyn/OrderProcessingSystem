# Order Processing System - Event-Driven Architecture Demo

A production-grade microservices system demonstrating event-driven architecture patterns built with .NET, RabbitMQ, PostgreSQL, and Redis.

## Architecture Overview
```
┌─────────────────┐     ┌──────────────────────────────┐
│   API Client    │────▶│        OrderService          │
└─────────────────┘     │  (CQRS + Event Sourcing)     │
                        └──────────────┬───────────────┘
                                       │ OrderPlaced
                        ┌──────────────▼───────────────────────────┐
                        │         RabbitMQ Exchange                 │
                        │      (order-processing-events)            │
                        └──┬──────────┬──────────┬─────────────────┘
                           │          │          │
               ┌───────────▼─┐  ┌─────▼──────┐  ├──────────────┐
               │  Inventory  │  │  Payment   │  │Notification  │
               │  Service    │  │  Service   │  │  Service     │
               └───────────┬─┘  └─────┬──────┘  └──────────────┘
                           │          │
                        ┌──▼──────────▼──┐
                        │ AnalyticsService│
                        └────────────────┘
```

## Event Flow (Saga Pattern)
```
OrderPlaced ──▶ InventoryReserved ──▶ PaymentProcessed ──▶ OrderConfirmed
     │                │                      │
     │         InventoryFailed          PaymentFailed
     │                │                      │
     └────────▶ OrderCancelled ◀─────────────┘
                                    (+ InventoryReleased)
```

## Services

| Service | Port | Responsibility |
|---------|------|----------------|
| OrderService | 5001 | Order management, CQRS, Event Sourcing |
| InventoryService | 5002 | Stock management, Reservations |
| PaymentService | 5003 | Payment processing |
| NotificationService | 5004 | Customer notifications |
| AnalyticsService | 5005 | Real-time analytics |

## Technology Stack

- **Runtime**: .NET / C#
- **Message Broker**: RabbitMQ (with management UI)
- **Database**: PostgreSQL (event store + read models)
- **Cache**: Redis
- **API Documentation**: Scalar
- **Testing**: xUnit, Moq, Testcontainers
- **ORM**: Entity Framework Core with Npgsql

## Event-Driven Patterns Implemented

- **Event Sourcing**: All order state changes stored as immutable events
- **CQRS**: Separate command/query models in OrderService
- **Saga Pattern**: Distributed transaction coordination across services
- **Compensating Transactions**: Inventory release on payment failure
- **Pub/Sub Messaging**: Topic-based exchange with RabbitMQ
- **Dead Letter Queue**: Failed messages handled gracefully

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Git

## Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/Tabgyn/OrderProcessingSystem
cd OrderProcessingSystem
```

Update each `appsettings.Development.json` with your local credentials.

### 2. Start Infrastructure
```bash
docker-compose up -d
```

### 3. Run All Services

Open 5 terminals and run each service:
```bash
dotnet run --project src/OrderService/OrderService.csproj
dotnet run --project src/InventoryService/InventoryService.csproj
dotnet run --project src/PaymentService/PaymentService.csproj
dotnet run --project src/NotificationService/NotificationService.csproj
dotnet run --project src/AnalyticsService/AnalyticsService.csproj
```

### 4. Place an Order

POST to OrderService Scalar UI (`http://localhost:5211/scalar/v1`):
```json
{
  "customerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "productName": "Laptop",
      "quantity": 1,
      "unitPrice": 999.99
    }
  ]
}
```

## API Endpoints

### OrderService
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/orders | Place a new order |
| GET | /api/orders/{orderId} | Get order by ID |
| GET | /api/orders/customer/{customerId} | Get customer orders |
| GET | /health | Health check |

### AnalyticsService
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/analytics/metrics/today | Today's metrics |
| GET | /api/analytics/metrics/range | Metrics by date range |
| GET | /api/analytics/events/recent | Recent events |
| GET | /health | Health check |

## Infrastructure Access

| Service | URL | Credentials |
|---------|-----|-------------|
| RabbitMQ UI | http://localhost:15672 | guest/guest |
| PostgreSQL | localhost:5432 | postgres/postgres |
| Redis | localhost:6379 | - |

## Running Tests
```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/OrderService.Tests/OrderService.Tests.csproj
dotnet test tests/InventoryService.Tests/InventoryService.Tests.csproj

# Integration tests (requires Docker)
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

## Project Structure
```
OrderProcessingSystem/
├── src/
│   ├── OrderService/
│   │   ├── Application/
│   │   │   ├── Commands/        # CQRS Write Side
│   │   │   └── Queries/         # CQRS Read Side
│   │   ├── Consumers/           # Event Handlers
│   │   ├── Controllers/         # REST API
│   │   ├── Data/                # EF Core + Migrations
│   │   ├── Domain/              # Domain Models
│   │   └── Infrastructure/      # RabbitMQ, Repositories
│   ├── InventoryService/
│   ├── PaymentService/
│   ├── NotificationService/
│   ├── AnalyticsService/
│   └── SharedKernel/
│       ├── Events/              # Event Contracts
│       ├── Messaging/           # IEventBus Interface
│       └── Models/              # Shared Models
├── tests/
│   ├── OrderService.Tests/      # Unit Tests
│   ├── InventoryService.Tests/  # Unit Tests
│   └── IntegrationTests/        # Integration Tests
├── docker-compose.yml
└── README.md
```

## Configuration Security

**IMPORTANT**: Never commit credentials to source control.

- Use `appsettings.Development.json` for local development (gitignored)
- Use environment variables or secret management for production:
```bash
RabbitMQ__Host=your-rabbitmq-host
RabbitMQ__Username=your-username
RabbitMQ__Password=your-password
ConnectionStrings__OrderDatabase=Host=...
```

## License

MIT