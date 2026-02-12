# Order Processing System - Event-Driven Architecture Demo

Real-time order processing system demonstrating event-driven architecture patterns using .NET, RabbitMQ, and PostgreSQL.

## Architecture

### Microservices
- **OrderService** - Handles order placement and publishes OrderPlaced events
- **InventoryService** - Manages inventory and reserves stock
- **PaymentService** - Processes payments
- **NotificationService** - Sends order confirmations and updates
- **AnalyticsService** - Real-time analytics and dashboards

### Infrastructure
- **Message Broker**: RabbitMQ
- **Event Store**: PostgreSQL
- **Cache**: Redis
- **Orchestration**: Docker Compose

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Git

## Getting Started

1. Clone the repository
```bash
git clone <repository-url>
cd OrderProcessingSystem
```

2. Start infrastructure services
```bash
docker-compose up -d
```

3. Build the solution
```bash
dotnet build
```

4. Run services (instructions coming in later stages)

## Infrastructure Access

- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **PostgreSQL**: localhost:5432 (postgres/postgres)
- **Redis**: localhost:6379

## Project Structure
```
OrderProcessingSystem/
├── src/
│   ├── OrderService/          # Order management service
│   ├── InventoryService/      # Inventory management service
│   ├── PaymentService/        # Payment processing service
│   ├── NotificationService/   # Notification service
│   ├── AnalyticsService/      # Analytics service
│   └── SharedKernel/          # Shared contracts and events
├── docker-compose.yml         # Infrastructure orchestration
└── README.md
```

## Event-Driven Patterns Implemented

- Event Sourcing
- CQRS (Command Query Responsibility Segregation)
- Saga Pattern for distributed transactions
- Dead Letter Queues
- Event Versioning

## Development Status

- [x] Stage 1: Project foundation and infrastructure setup
- [ ] Stage 2: Event contracts and shared infrastructure
- [ ] Stage 3: OrderService implementation
- [ ] Stage 4: Inventory and Payment services
- [ ] Stage 5: Notification and Analytics services
- [ ] Stage 6: Testing and documentation

## License

MIT