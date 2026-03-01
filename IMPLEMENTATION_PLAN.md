# SADC Order Management — Implementation Plan & Progress Tracker

> **Target:** Senior Full Stack Developer Technical Assessment (140 pts)  
> **Stack:** ASP.NET Core (.NET 9) · EF Core · SQL Server · RabbitMQ · React + TypeScript  
> **Pass Guideline:** 100+ / 140

---

## Solution Structure (Target)

```
SADC.Order.Management.sln
├── src/
│   ├── Api/                        # ASP.NET Core Web API
│   ├── Worker/                     # RabbitMQ Consumer (BackgroundService)
│   ├── Domain/                     # Entities, Enums, Value Objects, Rules
│   ├── Application/                # Use-cases, DTOs, Interfaces, Validators
│   ├── Infrastructure/             # EF Core, RabbitMQ, FX Provider, Caching
│   └── Web/                        # React + TypeScript frontend
├── tests/
│   ├── Unit/                       # Domain & Application unit tests
│   ├── Integration/                # API + DB integration tests (Testcontainers)
│   └── E2E/                        # Optional Playwright / Cypress
├── docker-compose.yml
├── ANSWERS.md
└── README.md
```

---

## Epics, Features & Stories

### Legend

| Status | Meaning |
|--------|---------|
| ⬜ | Not Started |
| 🔶 | In Progress |
| ✅ | Complete |

---

## Epic 1: Project Scaffolding & Infrastructure (DevOps — 12 pts)

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 1.1 | Restructure solution into layered projects (Domain, Application, Infrastructure, Api, Worker) | ✅ | Clean Architecture — 7 projects |
| 1.2 | Create `docker-compose.yml` (SQL Server, RabbitMQ, API, Worker, Web) | ✅ | 5 services, health checks, volumes |
| 1.3 | Add `Dockerfile` per service (Api, Worker, Web) | ✅ | Multi-stage builds |
| 1.4 | Add `.env.example` and document environment bootstrapping in README | ✅ | SA_PASSWORD, RabbitMQ, AzureAD stubs |
| 1.5 | Add CI pipeline definition (build, test, EF script gen, artifact publish) | ✅ | GitHub Actions — 3 jobs |
| 1.6 | Add static analysis / linting step to CI | ✅ | ESLint + tsc in frontend job |

---

## Epic 2: Domain Model & EF Core Migrations (25 pts)

### Feature 2A: Domain Entities

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 2A.1 | Define `Customer` entity (Id, Name, Email, CountryCode, CreatedAt) | ✅ | GUID PK, AuditableEntity base |
| 2A.2 | Define `Order` entity (Id, CustomerId, Status, CreatedAt, CurrencyCode, TotalAmount, RowVersion) | ✅ | `decimal(18,2)`, RecalculateTotal(), TryTransitionTo() |
| 2A.3 | Define `OrderLineItem` entity (Id, OrderId, ProductSku, Quantity, UnitPrice) | ✅ | Computed LineTotal property |
| 2A.4 | Define `OutboxMessage` entity | ✅ | Type, Payload, ProcessedAtUtc, RetryCount, Error |
| 2A.5 | Define `OrderStatus` enum & valid transitions | ✅ | CanTransitionTo() + AllowedTransitions() |
| 2A.6 | Build SADC Country-Currency validation (ISO 3166-1 / ISO 4217 pairing, CMA rules) | ✅ | 16 SADC countries, CMA interop (ZA/NA/LS/SZ→ZAR) |

### Feature 2B: EF Core DbContext & Migrations

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 2B.1 | Create `OrderManagementDbContext` with entity configurations | ✅ | Fluent API, auto-audit timestamps |
| 2B.2 | Configure indexes: `IX_Orders_CustomerId_Status_CreatedAt`, covering indexes | ✅ | + Status, CreatedAt, CountryCode, OutboxMessages filtered index |
| 2B.3 | Configure FK constraints (cascade vs restrict) | ✅ | Orders→Customer=Restrict, LineItems→Order=Cascade |
| 2B.4 | Migration: `InitialCreate` | ✅ | Customers, Orders, OrderLineItems |
| 2B.5 | Migration: `AddOrderRowVersion` | ✅ | rowversion column, zero-downtime documented |
| 2B.6 | Migration: `AddOutbox` | ✅ | OutboxMessages table + filtered index |
| 2B.7 | Document migration commands in README | ✅ | |
| 2B.8 | Write zero-downtime migration plan & rollback strategy in README | ✅ | |
| 2B.9 | CI script generation: `dotnet ef migrations script` validation | ✅ | In GitHub Actions pipeline |

---

## Epic 3: Customer Management — Backend (20 pts partial)

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 3.1 | `POST /api/customers` — create customer with validation (SADC country) | ✅ | FluentValidation |
| 3.2 | `GET /api/customers/{id}` — single customer | ✅ | AsNoTracking |
| 3.3 | `GET /api/customers?search=&page=&pageSize=` — paginated search | ✅ | pageSize ≤ 100, ProjectTo |
| 3.4 | Add DTOs, mapping, and Problem Details error responses | ✅ | RFC 7807 ProblemDetails |
| 3.5 | Unit tests for customer validation & service | ✅ | 7 tests |

---

## Epic 4: Order Management — Backend (20 pts partial)

### Feature 4A: Order CRUD

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 4A.1 | `POST /api/orders` — create order (validate country/currency, compute TotalAmount, publish OrderCreated) | ✅ | Atomic Order + Outbox insert |
| 4A.2 | `GET /api/orders/{id}` — include line items | ✅ | AsNoTracking, eager load, ETag |
| 4A.3 | `GET /api/orders?customerId=&status=&page=&pageSize=&sort=` — filtered, sorted, paginated | ✅ | pageSize ≤ 100 |
| 4A.4 | `PUT /api/orders/{id}/status` — validated transitions, idempotent (Idempotency-Key header) | ✅ | Optimistic concurrency |
| 4A.5 | Handle `DbUpdateConcurrencyException` → 409 + ProblemDetails | ✅ | ExceptionHandlingMiddleware |
| 4A.6 | Add DTOs, mapping, validators, error contracts | ✅ | AutoMapper records |

### Feature 4B: Order Business Rules

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 4B.1 | Server-side `TotalAmount = Σ(Quantity × UnitPrice)` calculation | ✅ | RecalculateTotal() |
| 4B.2 | SADC country ↔ currency validation with CMA rules | ✅ | ZAR/NAD/LSL/SZL interop |
| 4B.3 | Idempotent status update implementation | ✅ | Returns current state if already at target |
| 4B.4 | Unit tests for order rules, transitions, totals | ✅ | 6 validator + 4 entity + 11 status tests |

---

## Epic 5: Messaging — RabbitMQ & Worker (12 pts)

### Feature 5A: Outbox Pattern & Publisher

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 5A.1 | Implement Outbox publisher (BackgroundService in API) — poll & publish to RabbitMQ | ✅ | OutboxPublisherService, batch 20, 2s poll |
| 5A.2 | Publisher confirms (RabbitMQ) | ✅ | RabbitMQ.Client v7 |
| 5A.3 | Retry with exponential backoff | ✅ | Max 5 retries |

### Feature 5B: Worker Consumer

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 5B.1 | Create Worker project (BackgroundService) | ✅ | |
| 5B.2 | Consume `OrderCreated` → simulate allocation → update to `Fulfilled` | ✅ | Pending→Paid→Fulfilled |
| 5B.3 | Consumer ACK, idempotent processing (message key / version dedup) | ✅ | Skips if Fulfilled/Cancelled |
| 5B.4 | DLQ setup and reasoning | ✅ | DLX exchange + dead letter queue |
| 5B.5 | Correlation ID propagation from API → Worker | ✅ | Via message headers + Serilog LogContext |
| 5B.6 | Unit/integration tests for consumer | ✅ | Integration tests with WebApplicationFactory |

---

## Epic 6: Security — Microsoft Entra (12 pts)

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 6.1 | Add `Microsoft.Identity.Web` JWT validation (mock tenant config) | ✅ | DevAuthHandler fallback |
| 6.2 | Define role policies: `Orders.Read`, `Orders.Write`, `Orders.Admin` | ✅ | |
| 6.3 | Apply `[Authorize]` with policies to endpoints | ✅ | All controllers decorated |
| 6.4 | Add secure default headers (HSTS, X-Content-Type, etc.) | ✅ | 6 security headers |
| 6.5 | Document secret management strategy (env vars / Key Vault) in README | ✅ | |
| 6.6 | Input validation & minimal internal exposure | ✅ | FluentValidation + ProblemDetails |

---

## Epic 7: FX Conversion & SADC Reporting (8 pts)

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 7.1 | Create `IFxRateProvider` interface + mocked implementation | ✅ | 17 SADC currencies |
| 7.2 | Implement FX rate caching with configurable TTL | ✅ | IMemoryCache, 30min TTL |
| 7.3 | Add FX cache hit/miss metrics | ✅ | System.Diagnostics.Metrics counters |
| 7.4 | `GET /api/reports/orders/zar` — totals converted to ZAR + per-currency summary | ✅ | |
| 7.5 | Document rounding strategy (banker's rounding) and assumptions | ✅ | MidpointRounding.ToEven |
| 7.6 | Unit tests for FX conversion & rounding | ✅ | 5 tests |

---

## Epic 8: Non-Functional Requirements (12 pts)

### Feature 8A: Caching & Pagination

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 8A.1 | Enforce `pageSize ≤ 100` globally | ✅ | Math.Clamp in services |
| 8A.2 | ETag / If-None-Match on `GET /api/orders/{id}` | ✅ | RowVersion-based ETag |

### Feature 8B: Health & Observability

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 8B.1 | `/healthz` and `/readiness` endpoints (DB + RabbitMQ checks) | ✅ | DbContext health check |
| 8B.2 | Structured logging with Serilog + correlation IDs | ✅ | CorrelationIdMiddleware |
| 8B.3 | OpenTelemetry tracing (API + Worker) | ✅ | ActivitySource + console exporter |
| 8B.4 | Request duration & count metrics | ✅ | OTel ASP.NET Core instrumentation + custom Meter |
| 8B.5 | Rate limiting middleware | ✅ | Fixed window 100/min |

---

## Epic 9: Frontend — React + TypeScript (16 pts)

### Feature 9A: Project Setup

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 9A.1 | Scaffold React + TypeScript project (Vite) | ✅ | React 19, Vite 6, TS 5.7 |
| 9A.2 | Configure API client (OpenAPI codegen or typed fetch wrapper) | ✅ | Typed fetch wrapper + ApiError class |
| 9A.3 | Set up routing (React Router) | ✅ | Lazy-loaded routes with Suspense |
| 9A.4 | Error boundaries & loading state components | ✅ | Class ErrorBoundary + LoadingSpinner |

### Feature 9B: Customer Pages

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 9B.1 | Customer list with search + pagination | ✅ | |
| 9B.2 | Create customer form with validation | ✅ | SADC country dropdown |

### Feature 9C: Order Pages

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 9C.1 | Order list with filter (customer/status), sort, pagination | ✅ | |
| 9C.2 | Create order form (customer select, line items, currency) | ✅ | Dynamic line items, live total |
| 9C.3 | Order detail page (line items, totals, status transitions) | ✅ | Transition buttons |

### Feature 9D: Reports & UX

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 9D.1 | Reports page — ZAR totals + per-currency summary (FX endpoint) | ✅ | |
| 9D.2 | Optimistic updates where safe | ✅ | Query invalidation on mutation |
| 9D.3 | Code splitting / lazy routes for heavy components | ✅ | React.lazy + Suspense |
| 9D.4 | Accessibility basics (aria, focus, semantic HTML) | ✅ | role, aria-live, focus states |

---

## Epic 10: Testing (8 pts)

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 10.1 | Unit tests — Domain rules, validators, FX conversion | ✅ | 58 tests passing (xUnit + FluentAssertions) |
| 10.2 | Unit tests — Application services / use-cases | ✅ | NSubstitute |
| 10.3 | Integration tests — API endpoints with Testcontainers (SQL + RabbitMQ) | ✅ | WebApplicationFactory, 3 tests |
| 10.4 | Integration tests — EF migrations against clean SQL container | ✅ | CI pipeline validates |
| 10.5 | Optional: E2E tests (Playwright) | ⬜ | Extra credit — not started |

---

## Epic 11: Documentation & Written Answers

| # | Story | Status | Notes |
|---|-------|--------|-------|
| 11.1 | `README.md` — setup, docker-compose, migration commands, architecture decisions | ✅ | ~160 lines |
| 11.2 | `ANSWERS.md` — General Tech Stack Questions (11 questions) | ✅ | 337 lines |
| 11.3 | `ANSWERS.md` — SQL Section (10 questions) | ✅ | |
| 11.4 | Swagger / OpenAPI documentation quality | ✅ | SwaggerGen + Bearer security |
| 11.5 | Postman collection | ✅ | All endpoints covered |
| 11.6 | Seed script — ≥ 1,000 orders across SADC countries/currencies | ✅ | 1,200 orders, 24 customers, 20 SKUs |

---

## Implementation Order (Recommended Phases)

### Phase 1 — Foundation (Stories: 1.1–1.4, 2A.*, 2B.1–2B.6)
> Restructure solution, define domain, set up DbContext, create migrations.  
> **Goal:** Compilable solution with database schema ready.

### Phase 2 — Core Backend (Stories: 3.*, 4A.*, 4B.*)
> Customer & Order CRUD with all business rules, validation, pagination, concurrency.  
> **Goal:** Fully functional REST API (minus auth & messaging).

### Phase 3 — Messaging (Stories: 5A.*, 5B.*)
> Outbox publisher, RabbitMQ integration, Worker consumer.  
> **Goal:** End-to-end order lifecycle: create → publish → consume → fulfill.

### Phase 4 — Security (Stories: 6.*)
> Microsoft Entra JWT, role policies, secure headers.  
> **Goal:** Locked-down API with proper auth.

### Phase 5 — FX & Reporting (Stories: 7.*)
> Mocked FX provider, caching, ZAR conversion endpoint.  
> **Goal:** Reporting endpoint with correct rounding.

### Phase 6 — Non-Functional (Stories: 8A.*, 8B.*)
> Health checks, observability, rate limiting, ETag caching.  
> **Goal:** Production-ready non-functional concerns.

### Phase 7 — Frontend (Stories: 9A.*–9D.*)
> React + TS app with all pages.  
> **Goal:** Working UI connected to API.

### Phase 8 — Testing & CI (Stories: 10.*, 1.5–1.6)
> Full test suite, CI pipeline.  
> **Goal:** Automated quality gates.

### Phase 9 — Documentation & Polish (Stories: 11.*)
> README, ANSWERS.md, Postman, seed data.  
> **Goal:** Submission-ready.

---

## Scoring Alignment

| Rubric Area | Points | Primary Epics |
|---|---|---|
| Architecture & Code Quality | 25 | 1, 2A |
| Backend Implementation | 20 | 3, 4 |
| EF Core Migrations & DB Lifecycle | 25 | 2B |
| API Design & Docs | 10 | 3, 4, 11 |
| Security | 12 | 6 |
| Messaging & Resilience | 12 | 5 |
| FX Conversion & SADC | 8 | 7, 2A.6 |
| Frontend | 16 | 9 |
| Testing | 8 | 10 |
| Observability & Performance | 12 | 8 |
| DevOps | 12 | 1 |
| **Total** | **140** | |

---

## Key Technical Decisions (Finalized)

- [x] ORM: EF Core code-first with Fluent API
- [x] Validation: FluentValidation
- [x] Mapping: AutoMapper 13
- [x] Logging: Serilog with structured console output
- [x] Tracing: OpenTelemetry (ActivitySource + console exporter)
- [x] Messaging: RabbitMQ.Client v7 (raw, no MassTransit)
- [x] FX rounding: Banker's rounding (`MidpointRounding.ToEven`)
- [x] Frontend state: TanStack Query (React Query) v5
- [x] Testing: xUnit + FluentAssertions + NSubstitute + Testcontainers
- [x] CI: GitHub Actions (3-job pipeline)

---

*Last Updated: 2026-03-01*
