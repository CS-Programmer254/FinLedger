# FinLedger

# FinLedger - Enterprise Payment Processing System

A production-grade payment processing system demonstrating advanced domain-driven design principles, clean architecture, and financial system best practices. Built with ASP.NET Core 10, Entity Framework Core, and PostgreSQL, this system implements a complete payment lifecycle from creation through settlement with full auditability and reconciliation capabilities.

## Overview

FinLedger provides a robust foundation for payment processing applications, designed with enterprise requirements in mind. The system enforces financial integrity through double-entry ledger accounting, implements idempotent operations for reliability, and maintains complete audit trails through event sourcing. The architecture separates concerns across distinct layers while maintaining strong domain boundaries and business rule enforcement.

## Architecture

The system is organized into four primary layers following clean architecture principles:

### Domain Layer

The domain layer encapsulates all business logic and rules. It contains no external dependencies and serves as the foundation for the entire system. Key components include:

- Entity base class providing consistent identity semantics across all domain objects
- AggregateRoot base class managing transaction boundaries and domain events
- Payment aggregate root controlling the complete payment lifecycle
- LedgerEntry child entities representing individual ledger transactions
- WebhookAggregate managing webhook delivery lifecycle
- Value objects (Money, TransactionId, PaymentReference, MerchantId, EncryptedPayload) enforcing type safety and validation
- Domain events capturing all state changes for audit purposes
- Repository interfaces defining contracts for persistence operations

### Application Layer

The application layer orchestrates business operations through command handlers implementing the MediatR command bus pattern. This approach decouples request handling from HTTP concerns and enables flexible cross-cutting behaviors:

- Command objects representing business operations
- Command handlers implementing use case logic
- Pipeline behaviors for logging and validation
- Validators enforcing command-level constraints

### Infrastructure Layer

The infrastructure layer handles all external system integration:

- Entity Framework Core DbContext managing database operations
- EF Core configurations mapping aggregates to relational schema
- Repository implementations providing aggregate persistence
- Event store maintaining immutable audit log
- Type conversion and owned entity mappings for value objects

### API Layer

The API layer exposes RESTful endpoints for client integration:

- Versioned controllers providing endpoint structure
- Global exception handling middleware
- CORS configuration for client application access
- Health check endpoint for monitoring
- Swagger/OpenAPI documentation

## Key Features

### Double-Entry Ledger Accounting

Every payment transaction creates balanced ledger entries ensuring financial integrity. The system maintains an invariant where total customer debits equal clearing balance plus merchant credits. This accounting approach prevents ledger imbalance and immediately reveals any data corruption or inconsistencies.

### Idempotent Operations

The system guarantees safe operation replay through multiple mechanisms:

- Unique payment reference constraint prevents duplicate payment creation
- Webhook idempotency tracking ensures notification delivery without duplication
- Payment status state machine prevents invalid state transitions
- Atomic aggregate operations ensure consistency

### Event Sourcing

All state changes are captured as immutable domain events persisted to the event store. This approach provides:

- Complete audit trail of all operations
- Capability to replay events and reconstruct state
- Support for temporal queries
- Foundation for event-driven architectures

### Reconciliation Reporting

The system provides point-in-time reconciliation snapshots capturing payment counts and account balances. Reconciliation reports verify the ledger invariant and identify any discrepancies requiring investigation.

### Webhook Retry Logic

Failed webhook deliveries are automatically retried using exponential backoff strategy:

- Initial delivery occurs immediately
- Subsequent retries at 2, 4, 8, 16 second intervals
- Maximum 5 retry attempts
- Persisted delivery history for troubleshooting

## Technical Stack

- ASP.NET Core 10.0 - Web framework
- Entity Framework Core 10.0 - Object-relational mapping
- PostgreSQL 14+ - Relational database
- MediatR - Command bus and mediator pattern
- FluentValidation - Command validation
- Serilog - Structured logging
- Swagger/OpenAPI - API documentation

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- PostgreSQL 16 or later
- Docker and Docker Compose (optional)

### Local Development Setup

Clone the repository and navigate to the project directory:

```
git clone https://github.com/CS-Programmer254/finledger.git
cd finledger
```

Update the database connection string in appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=finledger;Username=postgres;Password=postgres;Port=5432"
  }
}
```

Build the solution:

```
dotnet build
```

Apply database migrations:

```
dotnet ef database update --startup-project FinLedger
```

Start the API server:

```
dotnet run --project FinLedger
```

The API will be available at HTTP/2 POST https://localhost:7103/api/v1/ with Swagger documentation at https://localhost:7103/swagger.

### Docker Deployment

A complete development stack can be deployed using Docker Compose:

```
docker-compose up
```

This starts PostgreSQL, the API server, Redis cache, and RabbitMQ message queue.

## API Endpoints

### Payment Operations

Create a new payment:

```
POST /api/v1/payments
Content-Type: application/json

{
  "merchantId": "guid",
  "amount": 50000,
  "currency": "KES",
  "reference": "REF-12345",
  "webhookUrl": "https://merchant.example.com/webhook"
}

Response: 201 Created
{
  "paymentId": "guid",
  "status": "PENDING",
  "reference": "REF-12345",
  "createdAt": "2026-01-14T10:00:00Z"
}
```

Complete a payment through webhook callback:

```
POST /api/v1/webhooks/callback
Content-Type: application/json

{
  "reference": "REF-12345"
}

Response: 200 OK
{
  "paymentId": "guid",
  "status": "COMPLETED",
  "completedAt": "2026-01-14T10:05:00Z"
}
```

Generate reconciliation report:

```
GET /api/v1/reconciliation

Response: 200 OK
{
  "totalPayments": 100,
  "pendingPayments": 5,
  "completedPayments": 95,
  "failedPayments": 0,
  "customerBalance": 2500000,
  "clearingBalance": 0,
  "merchantBalance": 2500000,
  "isBalanced": true,
  "generatedAt": "2026-01-14T10:00:00Z"
}
```

Health check:

```
GET /health

Response: 200 OK
{
  "status": "Healthy",
  "checks": {
    "database": {"status": "Healthy"},
    "api": {"status": "Healthy"}
  },
  "timestamp": "2026-01-14T10:00:00Z"
}
```

## Payment Flow

The payment lifecycle consists of distinct phases:

Reserve Phase: Merchant initiates payment through POST /api/v1/payments. System validates input, creates Payment aggregate, and reserves funds in the clearing account through ledger entries. Payment status is set to PENDING. Domain events PaymentCreatedEvent and FundsReservedEvent are published.

Processing Phase: External payment gateway processes the payment. No system interaction occurs during this phase.

Settlement Phase: Gateway confirms payment completion through webhook callback. System transitions payment to COMPLETED status and settles funds to merchant account. Settlement ledger entries are created. Domain events PaymentCompletedEvent and FundsSettledEvent are published.

Reconciliation Phase: System generates periodic reconciliation reports verifying ledger balance and identifying any discrepancies.

## Data Model

The core data model represents payment processing operations:

Payments table contains the payment aggregate root with merchant reference, amount, currency, status, and timestamps. LedgerEntries table maintains individual debit and credit entries referenced to payments, supporting multi-entry accounting. WebhookAggregates and WebhookDeliveries track notification delivery attempts with retry history. Events table provides immutable event log for audit purposes. ReconciliationSnapshots capture periodic balance verification.

## Testing

Unit tests verify domain entity behavior and invariants:

```
dotnet test tests/Domain.Tests
```

Integration tests validate application handler behavior with in-memory database:

```
dotnet test tests/Application.Tests
```

API tests verify endpoint contracts and HTTP behavior:

```
dotnet test tests/Integration.Tests
```

## Deployment

For production deployment, update configuration for your environment:

Replace localhost connection strings with production database
Configure appropriate logging levels
Enable HTTPS enforcement
Set up database backups and replication
Configure monitoring and alerting
Update CORS policies for production origins
Use environment-specific configuration files

## Design Patterns and Principles

The system demonstrates several architectural patterns:

Clean Architecture separates concerns across layers with dependency flow inward. Domain layer contains pure business logic with no framework dependencies. Application layer orchestrates use cases. Infrastructure implements persistence details. API layer exposes HTTP contracts.

Domain-Driven Design places business logic in aggregates rather than services. Value objects enforce type safety. Domain events capture business-relevant state changes. Repository pattern abstracts persistence.

Command Query Responsibility Segregation separates write operations through commands from read operations through queries. Commands return results for confirmation. Queries serve reporting without side effects.

Event Sourcing maintains immutable event log. Aggregate state reconstructs from events. Temporal queries determine state at specific points in time. Event replay supports debugging and analysis.

Repository Pattern abstracts data access behind interfaces. Aggregates are loaded and saved as units. Child entities are not accessible independently. This enforces aggregate boundaries and consistency.

## Security Considerations

Webhook payloads are encrypted using AES-256-GCM providing authenticated encryption. Payment references serve as idempotency keys preventing duplicate operations. Database constraints enforce business rule invariants at the storage layer. Unique indexes prevent invalid data states. All state transitions validate against business rules before persistence.

## Performance Optimization

Database indexes on frequently queried columns enable efficient lookups. Event store is append-only optimizing write performance. Value object queries do not require additional round trips. Repository implementations batch operations where appropriate. Serialized transactions use SERIALIZABLE isolation for critical operations.

## Monitoring and Observability

Structured logging via Serilog captures all significant operations. Health check endpoint monitors system status. Event store provides complete operation history. Reconciliation reports identify discrepancies. Exception handling middleware logs all errors.

## Contributing

Development follows established patterns and conventions:

Maintain separation of concerns across layers
Implement domain logic in aggregates, not handlers
Write tests for new features
Update documentation for API changes
Follow naming conventions and code style
Ensure backward compatibility for API changes

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Contact

For questions or issues, please open an issue on the GitHub repository or contact the development team.
