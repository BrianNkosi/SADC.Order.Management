Senior Developer Technical Assessment
i
Senior Developer
Technical Assessment
Senior Developer Technical Assessment
i
Senior Full Stack Developer— Take-Home Assessment
Timebox: 7 Calendar days Submission: Git repo URL or zipped project + ANSWERS.md with written responses
Goal
Demonstrate senior/principal-level capability across:
•
Architecture & Design: scalable, secure, maintainable systems; tradeoff reasoning
•
Backend: ASP.NET Core (.NET 8), EF Core (code-first), SQL Server
•
Frontend: React + TypeScript
•
Messaging: RabbitMQ and reliable event publication (Outbox)
•
Security: Microsoft Entra JWT & policies
•
Testing: unit, integration, optional E2E
•
CI/CD: build, test, artifacts, environment bootstrapping
•
Observability: logging, metrics, tracing
•
Performance & Resilience
•
Regional requirements: SADC countries & currencies, FX conversion with correct rounding
Business Scenario
Build a SADC Order Management system:
•
Manage Customers, Orders, OrderLineItems.
•
Order statuses: Pending → Paid → Fulfilled → Cancelled.
•
On order creation, publish OrderCreated to RabbitMQ; a Worker consumes and simulates downstream allocation, moving to Fulfilled.
•
Validate SADC country (ISO 3166-1 alpha-2) and currency (ISO 4217) pairing: e.g., ZA → ZAR, BW → BWP, ZW → ZWL and USD, etc. Consider Common Monetary Area (CMA) relationships (ZAR, NAD, LSL, SZL).
Advanced (mandatory for 10+ years):
•
Implement FX conversion to ZAR for reporting. Use a mocked rates provider and caching; handle rounding (banker’s rounding or specify strategy) and clearly document assumptions. Expose an endpoint to get order totals in ZAR and per-currency summaries.
Senior Developer Technical Assessment
ii
Functional Requirements
Data Model (EF Core — Code First)
•
Customer: Id, Name, Email, CountryCode, CreatedAt
•
Order: Id, CustomerId, Status, CreatedAt, CurrencyCode, TotalAmount, LineItems[], RowVersion (SQL Server rowversion)
•
OrderLineItem: Id, OrderId, ProductSku, Quantity (>0), UnitPrice (≥0, in order currency)
Rules
•
Server-side TotalAmount = Σ(Quantity × UnitPrice).
•
Validate SADC country/currency pairing.
•
Enforce valid status transitions; implement idempotent status updates (header: Idempotency-Key).
•
Add indexes: IX_Orders_CustomerId_Status_CreatedAt, plus any covering indexes you deem necessary.
•
Use AsNoTracking() for reads; avoid N+1 queries.
•
FX conversion endpoint(s): return totals in ZAR and per-currency breakdown; cache rates and specify TTL.
REST Endpoints (minimum)
•
POST /api/customers
•
GET /api/customers/{id}
•
GET /api/customers?search=&page=&pageSize=
•
POST /api/orders # validates country/currency, computes totals, publishes OrderCreated
•
GET /api/orders/{id} # includes line items
•
GET /api/orders?customerId=&status=&page=&pageSize=&sort=
•
PUT /api/orders/{id}/status # validated transition, idempotent via Idempotency-Key
•
GET /api/reports/orders/zar # totals converted to ZAR + per-currency summary (NEW, mandatory)
GraphQL (Optional)
•
Read-only GraphQL to query nested orders/line items, filter/sort. Discuss N+1 avoidance (DataLoader), schema design, and when GraphQL is preferable.
Auth & Security (Microsoft Entra)
•
JWT validation with Microsoft.Identity.Web (mock allowed if tenant not available).
•
Role policies: Orders.Read, Orders.Write, Orders.Admin.
•
Secure defaults: headers, validation, minimal exposure of internals.
•
Secret management strategy (documented), e.g., env vars or Key Vault (describe).
Senior Developer Technical Assessment
iii
Messaging (RabbitMQ)
•
Outbox pattern: atomically write Order and Outbox message, then publish asynchronously.
•
Publisher confirms and consumer ack; implement retry with backoff and DLQ reasoning/implementation.
•
Idempotent consumer: deduplicate using message keys/version.
Non-Functional Requirements
•
Pagination: enforce pageSize ≤ 100.
•
Caching: ETag or If-None-Match on GET /api/orders/{id}.
•
Health endpoints: /healthz, /readiness.
•
Logging: structured logs with correlation IDs propagated to Worker.
•
Metrics: request duration, counts, and basic FX cache hit/miss.
•
Rate limiting (ASP.NET Core middleware or your design): at least basic protection.
Frontend (React + TypeScript)
Pages:
•
Customers: list (search + paginate) + create
•
Orders: list (filter by customer/status; sort; paginate) + create
•
Order Details: show line items, totals, status transitions
•
Reports: totals in ZAR and per-currency summary (uses FX endpoint)
Guidelines:
•
Strong TS types (OpenAPI codegen, Zod, or your approach).
•
Error boundaries, loading states, accessibility basics.
•
Local caching (SWR/RTK Query) and optimistic updates (where safe).
•
Bundle/code splitting where high cost components exist.
Senior Developer Technical Assessment
iv
EF Core Migrations & Database Lifecycle (Mandatory, Enhanced)
This section is pivotal for a 10+ year senior.
Objectives
•
Design, evolve, and operate schema safely with EF Core migrations.
•
Provide a zero-downtime migration plan and rollback strategy.
•
Automate script generation and validation in CI.
•
Demonstrate concurrency and indexing best practices.
Deliverables
1.
Migrations (src/Api/Migrations/):
o
InitialCreate: Customers, Orders, OrderLineItems, FKs, indexes, proper money precision.
o
Evolution migrations:
▪
AddOrderRowVersion (rowversion for optimistic concurrency)
▪
AddOutbox (reliable messaging)
▪
Optional: AddFxRatesCache (if persisted)
2.
Commands (README):
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/Api/Api.csproj -s src/Api/Api.csproj
dotnet ef database update -p src/Api/Api.csproj -s src/Api/Api.csproj
dotnet ef migrations add AddOrderRowVersion -p src/Api/Api.csproj -s src/Api/Api.csproj
dotnet ef migrations add AddOutbox -p src/Api/Api.csproj -s src/Api/Api.csproj
dotnet ef database update -p src/Api/Api.csproj -s src/Api/Api.csproj
3.
Script generation:
dotnet ef migrations script -p src/Api/Api.csproj -s src/Api/Api.csproj -o migrations.sql
4.
Zero-downtime plan (README, 2–3 paragraphs):
o
Additive first (nullable columns, defaults), backfill via job, then tighten constraints.
o
Avoid breaking changes (renames/drops) until consumers updated.
o
Deployment order: apply migration → deploy app (blue/green/canary).
o
Long backfills: batch size, lock minimization; consider READ COMMITTED SNAPSHOT.
5.
Rollback strategy:
o
dotnet ef database update <previousMigration>
Senior Developer Technical Assessment
v
o
Backups & restore notes for irreversible transformations; feature flags to disable features quickly.
6.
CI integration:
o
Generate EF script; validate against a clean SQL container.
o
Run integration tests after migrations applied.
7.
Indexes & constraints:
o
IX_Orders_CustomerId_Status_CreatedAt.
o
Precision: decimal(18,2) for money.
o
FK constraints with appropriate cascades or restricted deletes (audit-friendly).
8.
Optimistic concurrency:
o
rowversion on Orders; handle DbUpdateConcurrencyException with 409 and Problem Details.
9.
Outbox table:
C#
public class OutboxMessage {
public Guid Id { get; set; }
public string AggregateType { get; set; } = "Order";
public Guid AggregateId { get; set; }
public string Type { get; set; } = "OrderCreated";
public string Payload { get; set; } = "{}";
public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
public DateTime? ProcessedAtUtc { get; set; }
public int Version { get; set; } = 1;
}
Document atomic insert (Order + Outbox) and async publisher marking rows processed.
Senior Developer Technical Assessment
vi
General Tech Stack Questions (Answer in ANSWERS.md)
1.
Architecture: How would you evolve this service to support 50k orders/min peak writes and multi-region reads? Discuss sharding/partitioning, queue scaling, and potential CQRS.
2.
ASP.NET Core: Minimal APIs vs controllers in a medium-to-large codebase; filters/pipelines; versioning; rate limiting.
3.
EF Core: Tracking vs AsNoTracking(), batch updates, rowversion usage; strategies to avoid long-running locks during backfills.
4.
API Design: REST vs GraphQL in this domain; schema versioning; preventing N+1; error contracts with Problem Details.
5.
Security: Microsoft Entra roles/scopes, claims mapping; token lifetimes; defense-in-depth (headers, validation, secret hygiene).
6.
Messaging: RabbitMQ—exchange/queue topology, publisher confirms, DLX, retry/backoff; Outbox vs transactional messaging; idempotency patterns.
7.
Frontend (React + TS): Design for performance (memoization, virtualization), code-splitting, state management choices; contract typing and drift prevention.
8.
Testing/Quality: Test pyramid; integration tests with containers; E2E scope; CI gates (coverage thresholds, static analysis, supply chain security).
9.
Observability: OpenTelemetry tracing, log correlation across API & Worker; metrics; SLIs/SLOs and alerting.
10.
Performance & Resilience: Backpressure strategies; DB indexing and SARGability; caching; circuit breakers/timeouts; chaos testing (how would you approach?).
11.
Data & Compliance: Data retention and deletion; audit trails; regional data considerations (e.g., POPIA context awareness).
Senior Developer Technical Assessment
vii
SQL Section (SQL Server) — Senior/Principal Level (Answer in ANSWERS.md)
1.
Pagination Query Write a parameterized query to list a customer’s orders with status, createdAt, totalAmount, sorted by createdAt DESC, and a separate COUNT(*). Show @CustomerId, @Offset, @PageSize.
2.
Top Spenders (last 90 days) Top 10 customers by total spend over last 90 days; include customers with zero orders. Use LEFT JOIN, COALESCE, proper date filtering.
3.
Indexing Propose indexes for (CustomerId, Status, CreatedAt) queries; compare covering index vs clustered index choices. Discuss SARGability and pitfalls.
4.
Execution Plan & Key Lookups Given a join between Orders and OrderLineItems with filters on CustomerId and Status, explain identifying key lookups and removing them (included columns, reshaping index).
5.
Optimistic Concurrency Add rowversion and show EF Core update code that gracefully handles DbUpdateConcurrencyException with a retry or user feedback strategy.
6.
Deadlocks Provide a reader/writer deadlock scenario and a mitigation (consistent access order, shorter transactions, appropriate index, READ COMMITTED SNAPSHOT).
7.
Window Functions Running total per customer: SUM(...) OVER (PARTITION BY CustomerId ORDER BY CreatedAt); explain ordering/stability, tie breakers.
8.
Partitioning Strategy For millions of orders, propose partitioning (e.g., monthly by CreatedAt), and discuss partition switching for archival and its operational steps.
9.
Outbox Pattern Minimal Outbox schema; show how to atomically write Order + Outbox, and how the publisher safely moves messages to RabbitMQ with deduplication.
10.
Stored Procedure — Transaction Report (Detailed “one-view” + summary) Create sp_GetTransactionReport with params:
o
@StartDate DATE, @EndDate DATE, @Status NVARCHAR(20) = NULL, @CustomerId UNIQUEIDENTIFIER = NULL
Result Set 1: customer + order + line item details Result Set 2: TotalOrders, GrandTotalAmount
Include performance notes (indexes, filtered predicates, potential OPTION(RECOMPILE) for parameter sniffing, if justified) and return data sorted CreatedAt DESC.
Senior Developer Technical Assessment
viii
Scoring Rubric (140 points)
•
Architecture & Code Quality (25) Clear modular design; SOLID; clean boundaries; thoughtful tradeoffs.
•
Backend Implementation (20) Correct modeling, validations, robust endpoints; clean error contracts.
•
EF Core Migrations & DB Lifecycle (25) Clear migrations, script generation, zero-downtime + rollback plan, concurrency handling, indexes/constraints, CI validation.
•
API Design & Docs (10) Versioning, pagination/sorting, Swagger/OpenAPI quality, Problem Details.
•
Security (12) JWT validation (mock or real), policy roles, secure defaults, secret hygiene.
•
Messaging & Resilience (12) Outbox pattern, publisher confirms, retries/backoff, DLQ approach, idempotent consumer.
•
FX Conversion & SADC Handling (8) Accurate multi-currency handling; conversion to ZAR with caching; rounding strategy; per-currency summary.
•
Frontend (16) TS types, structure, UX, accessibility, client caching, code splitting.
•
Testing (8) Unit/integration coverage; meaningful tests; optional E2E gets extra credit.
•
Observability & Performance (12) Correlation IDs, metrics, tracing; rate limiting; indexes; caching; backpressure.
•
DevOps (12) CI pipeline, Docker compose, environment bootstrapping; security/static analysis; artifacts.
Pass guideline: 100+ / 140 for 7+ years.
Red flags: Unsafe migrations or no rollback; brittle messaging; weak security; lack of observability; poor currency handling; no pagination or idempotency.
Senior Developer Technical Assessment
ix
Getting Started (Candidate Steps)
1.
Create repo structure for API, Worker, Web.
2.
Docker Compose: boot SQL Server + RabbitMQ + API + Worker + Web.
3.
Implement features, add tests, and document decisions in README.
4.
Seed ≥1,000 orders across SADC countries/currencies.
5.
Provide Postman collection and Swagger.
6.
Answer General Tech & SQL questions in ANSWERS.md.
7.
Submit repo link or zip.
Notes & Assumptions
•
Microsoft Entra can be mocked; include real config stubs and policy examples.
•
FX: use a mocked provider; document data source assumptions, caching TTLs, rounding, and error handling.
•
Prioritize correctness, operability, and clarity over breadth. State tradeoffs explicitly.