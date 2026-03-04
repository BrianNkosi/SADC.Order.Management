# SADC Order Management System

A full-stack order management system for SADC (Southern African Development Community) regional trade.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 9 (.NET 9) |
| ORM | Entity Framework Core 9 (Code-First) |
| Database | SQL Server 2022 |
| Messaging | RabbitMQ 3 (raw `RabbitMQ.Client` v7) |
| Authentication | Microsoft Entra ID (Azure AD) via `Microsoft.Identity.Web` |
| Frontend | React 19 + TypeScript + Vite |
| State Management | TanStack Query (React Query) v5 |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Logging | Serilog (structured, console sink) |
| Testing | xUnit + FluentAssertions + NSubstitute + Testcontainers |
| CI | GitHub Actions |
| Orchestration | .NET Aspire 9 (AppHost + ServiceDefaults) |
| Containers | Podman (local dev via Aspire) / Docker (CI) |
| Observability | OpenTelemetry (OTLP) via Aspire Dashboard |

## Architecture

Clean Architecture with the following layers:

```
src/
├── Domain/           # Entities, Enums, Value Objects (no dependencies)
├── Application/      # Use-cases, DTOs, Interfaces, Validators
├── Infrastructure/   # EF Core, RabbitMQ, FX Provider, Caching
├── Api/              # ASP.NET Core Web API (controllers, middleware)
├── Worker/           # RabbitMQ consumer (BackgroundService)
├── Web/              # React + TypeScript SPA
├── AppHost/          # .NET Aspire orchestrator (SQL Server, RabbitMQ, services)
└── ServiceDefaults/  # Shared Aspire config (OpenTelemetry, health checks, resilience)
tests/
├── Unit/             # xUnit unit tests (58 tests)
└── Integration/      # Integration tests with WebApplicationFactory
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 22+](https://nodejs.org/)
- [Podman Desktop](https://podman-desktop.io/) or [Podman CLI](https://podman.io/) (for local dev via Aspire)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for docker-compose / CI)

### Quick Start (.NET Aspire + Podman) — Recommended

.NET Aspire orchestrates all infrastructure (SQL Server, RabbitMQ) and application services (API, Worker, Web) with a single command. The Aspire Dashboard provides built-in observability (traces, metrics, logs).

```bash
# 1. Clone and navigate
git clone <repo-url>
cd SADC.Order.Management

# 2. Ensure Podman is running
podman machine start   # on Windows/macOS

# 3. Set Podman as the container runtime for Aspire
$env:DOTNET_ASPIRE_CONTAINER_RUNTIME = "podman"   # PowerShell
# export DOTNET_ASPIRE_CONTAINER_RUNTIME=podman    # bash/zsh

# 4. Set the SQL Server password (Aspire user-secret)
cd src/AppHost
dotnet user-secrets set "Parameters:sql-password" "YourStrong@Passw0rd"
cd ../..

# 5. Start everything
dotnet run --project src/AppHost

# 6. Access the application
# Aspire Dashboard: https://localhost:17180  (shown in terminal output)
# API Swagger:      http://localhost:<port>/swagger  (port assigned by Aspire)
# Frontend:         http://localhost:5173
# RabbitMQ Mgmt:    http://localhost:<port> (port assigned by Aspire)
```

> **Note:** Aspire automatically manages SQL Server and RabbitMQ containers via Podman, applies EF Core migrations, and wires up OpenTelemetry with OTLP export to the Aspire Dashboard.

### Alternative: Docker Compose

Docker Compose is kept for CI pipelines and production-like deployments.

```bash
# 1. Copy environment variables
cp .env.example .env

# 2. Start all services
docker-compose up -d

# 3. Access the application
# API Swagger: http://localhost:5120/swagger
# Frontend:    http://localhost:5173
# RabbitMQ:    http://localhost:15672 (guest/guest)
```

### Local Development (Without Aspire)

If you want to run services individually without Aspire orchestration:

```bash
# Start infrastructure containers (Podman or Docker)
podman run -d --name sadc-sqlserver -e ACCEPT_EULA=Y -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
podman run -d --name sadc-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Backend
dotnet restore SADC.Order.Management.sln
dotnet build SADC.Order.Management.sln

# Run API
cd src/Api
dotnet run

# Run Worker (separate terminal)
cd src/Worker
dotnet run

# Frontend (separate terminal)
cd src/Web
npm install
npm run dev
```

### Running Tests

```bash
# All tests
dotnet test SADC.Order.Management.sln

# Unit tests only
dotnet test tests/Unit/SADC.Order.Management.Tests.Unit.csproj --verbosity normal

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/Integration/SADC.Order.Management.Tests.Integration.csproj
```

## EF Core Migrations

### Commands

```bash
# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure/SADC.Order.Management.Infrastructure.csproj \
  --startup-project src/Api/SADC.Order.Management.Api.csproj \
  --output-dir Persistence/Migrations

# Apply migrations
dotnet ef database update \
  --project src/Infrastructure/SADC.Order.Management.Infrastructure.csproj \
  --startup-project src/Api/SADC.Order.Management.Api.csproj

# Generate idempotent SQL script (for CI/CD)
dotnet ef migrations script --idempotent \
  --project src/Infrastructure/SADC.Order.Management.Infrastructure.csproj \
  --startup-project src/Api/SADC.Order.Management.Api.csproj \
  --output migrations.sql

# Rollback to a specific migration
dotnet ef database update <PreviousMigrationName> \
  --project src/Infrastructure/SADC.Order.Management.Infrastructure.csproj \
  --startup-project src/Api/SADC.Order.Management.Api.csproj
```

### Zero-Downtime Migration Strategy

1. **Pre-deployment**: Generate idempotent migration script via CI (`dotnet ef migrations script --idempotent`)
2. **Schema changes**: Apply additive changes first (new columns with defaults, new tables)
3. **Application deployment**: Deploy new code that works with both old and new schema
4. **Post-deployment**: Remove deprecated columns/constraints in a follow-up migration
5. **Rollback plan**: Each migration has a corresponding `Down()` method; rollback via `dotnet ef database update <PreviousMigration>`
6. **Validation**: CI generates and validates migration scripts against a clean SQL Server container

## API Endpoints

| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| POST | `/api/customers` | Create customer | Authenticated |
| GET | `/api/customers/{id}` | Get customer by ID | Authenticated |
| GET | `/api/customers?search=&page=&pageSize=` | Search customers | Authenticated |
| POST | `/api/orders` | Create order | Orders.Write |
| GET | `/api/orders/{id}` | Get order with line items | Orders.Read |
| GET | `/api/orders?customerId=&status=&page=&pageSize=&sortBy=&descending=` | List orders | Orders.Read |
| PUT | `/api/orders/{id}/status` | Update order status | Orders.Write |
| GET | `/api/reports/orders/zar` | ZAR conversion report | Orders.Read |
| GET | `/health` | Health check (all) | Anonymous |
| GET | `/alive` | Liveness check | Anonymous |

## Key Design Decisions

### SADC Country-Currency Validation
- All 16 SADC member states supported with ISO 3166-1 / ISO 4217 pairing
- **CMA (Common Monetary Area) rules**: ZA, NA, LS, SZ can use ZAR; NA also accepts NAD; LS accepts LSL; SZ accepts SZL
- Validation enforced at both API (FluentValidation) and domain (value object) layers

### Outbox Pattern
- Orders are created atomically with an `OutboxMessage` in a single EF Core transaction
- A `BackgroundService` polls the outbox table and publishes to RabbitMQ with publisher confirms
- Messages are marked as processed after successful publication, ensuring at-least-once delivery

### FX Conversion
- `IFxRateProvider` interface with mock implementation using realistic SADC currency rates
- In-memory caching with configurable TTL (default: 5 minutes)
- **Banker's rounding** (`MidpointRounding.ToEven`) for all currency conversions to minimize systematic bias

### Optimistic Concurrency
- `RowVersion` (SQL Server `rowversion`) on the Order entity
- `DbUpdateConcurrencyException` → HTTP 409 Conflict with ProblemDetails response

### Secret Management Strategy
- **Development**: User secrets (`dotnet user-secrets`) + `appsettings.Development.json`
- **Production**: Environment variables injected via Docker/Kubernetes
- **Recommendation**: Azure Key Vault with managed identity for production deployments
- Connection strings and API keys are never committed to source control

## Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `DOTNET_ASPIRE_CONTAINER_RUNTIME` | Container runtime for Aspire | `podman` |
| `SA_PASSWORD` | SQL Server SA password | `YourStrong!Passw0rd` |
| `RABBITMQ_DEFAULT_USER` | RabbitMQ username | `guest` |
| `RABBITMQ_DEFAULT_PASS` | RabbitMQ password | `guest` |
| `AzureAd__TenantId` | Azure AD tenant ID | `your-tenant-id` |
| `AzureAd__ClientId` | Azure AD client ID | `your-client-id` |
| `AzureAd__Audience` | Azure AD audience | `api://sadc-order-management` |
