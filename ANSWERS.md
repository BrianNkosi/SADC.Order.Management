# ANSWERS.md — Technical Assessment Responses

## Section A: General Tech Stack Questions

### 1. How would you structure a full-stack project for scalability and maintainability?

I use **Clean Architecture** with clearly separated layers:

- **Domain**: Pure C# — entities, enums, value objects, domain rules. Zero external dependencies.
- **Application**: Use-case orchestration, DTOs, interfaces (ports), validators. References only Domain.
- **Infrastructure**: Concrete implementations — EF Core, RabbitMQ, caching. References Application and Domain.
- **API / Worker**: Thin composition roots that wire dependencies via DI.
- **Frontend**: React SPA in its own project with typed API clients.

This structure ensures:
- **Testability**: Domain and Application layers are unit-testable without infrastructure.
- **Scalability**: Services can be extracted into microservices by splitting along bounded contexts.
- **Maintainability**: Changes to infrastructure (e.g., swapping SQL Server for PostgreSQL) don't affect business logic.

For the frontend, I co-locate components with their hooks/api calls and use code splitting via `React.lazy()` for page-level routes.

### 2. Explain your approach to error handling in a REST API.

I implement a **global exception handling middleware** that catches all unhandled exceptions and maps them to RFC 7807 **ProblemDetails** responses:

| Exception Type | HTTP Status | Scenario |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | Invalid input |
| `KeyNotFoundException` | 404 | Resource not found |
| `InvalidOperationException` | 422 | Business rule violation |
| `DbUpdateConcurrencyException` | 409 | Optimistic concurrency conflict |
| Unhandled `Exception` | 500 | Unexpected server error |

Key principles:
- Never expose internal implementation details (stack traces, SQL errors) in production.
- Always return structured JSON (`ProblemDetails`) with a `type`, `title`, `status`, and `detail` property.
- Log the full exception server-side with structured logging (Serilog) including correlation IDs.
- Use `[ProducesResponseType]` attributes for OpenAPI documentation of all error responses.

### 3. How do you ensure database consistency in a distributed system?

1. **Outbox Pattern**: Instead of publishing messages to RabbitMQ directly, I write an `OutboxMessage` in the same EF Core transaction as the business entity. A background service polls the outbox and publishes to RabbitMQ with publisher confirms. This ensures **at-least-once delivery** without 2PC.

2. **Idempotent Consumers**: The Worker checks if a message has already been processed (by event ID or order version) before applying state transitions, preventing duplicate processing.

3. **Optimistic Concurrency**: SQL Server `rowversion` column on orders ensures that concurrent updates are detected via `DbUpdateConcurrencyException` → HTTP 409.

4. **Atomic Transactions**: EF Core's `SaveChangesAsync` wraps all related writes (Order + LineItems + OutboxMessage) in a single transaction.

### 4. Describe your strategy for implementing authentication and authorization.

I use **Microsoft Entra ID (Azure AD)** with OAuth 2.0 / OpenID Connect:

- **Authentication**: `Microsoft.Identity.Web` validates JWT bearer tokens issued by Azure AD. The API is configured as a protected resource with a specific audience.
- **Authorization**: Role-based policies defined via `AddAuthorizationBuilder()`:
  - `Orders.Read` — view orders/reports (requires `Orders.Read`, `Orders.Write`, or `Orders.Admin` role)
  - `Orders.Write` — create/modify orders (requires `Orders.Write` or `Orders.Admin` role)
  - `Orders.Admin` — administrative operations (requires `Orders.Admin` role)
- **Controller-level**: `[Authorize]` attribute on all controllers; specific policies on individual actions.
- **Security headers**: HSTS, CSP, X-Content-Type-Options, X-Frame-Options, Permissions-Policy.

For development, a mock tenant configuration allows testing without a real Azure AD instance.

### 5. How would you implement caching in an API?

Multi-layer caching strategy:

1. **HTTP Caching (ETag)**: `GET /api/orders/{id}` returns an ETag based on the entity's last-modified timestamp. Subsequent requests with `If-None-Match` return 304 Not Modified.

2. **Application-Level Caching**: `IMemoryCache` with configurable TTL for expensive computations like FX rates. Cache keys are scoped by currency pair. Cache hit/miss metrics should be tracked.

3. **Response Compression**: For large paginated responses, enable Brotli/gzip compression.

4. **Rate Limiting**: ASP.NET Core built-in rate limiter (fixed window: 100 requests/minute) to protect against abuse.

In production, I'd add **Redis** as a distributed cache for multi-instance deployments and use `IDistributedCache` abstraction.

### 6. What testing strategies do you use?

**Testing Pyramid approach:**

1. **Unit Tests** (58 tests): Domain logic, validators, value objects, FX conversion, status transitions. Fast, isolated, no infrastructure.

2. **Integration Tests**: `WebApplicationFactory` with in-memory database substitution tests full HTTP pipeline (controllers → services → DB). For realistic tests, Testcontainers spins up actual SQL Server + RabbitMQ containers.

3. **Architecture Tests** (recommended addition): Verify layer dependencies (e.g., Domain should not reference Infrastructure).

4. **E2E Tests** (optional): Playwright/Cypress for critical user flows.

Tools: xUnit (runner), FluentAssertions (readable assertions), NSubstitute (mocking).

### 7. How do you handle logging and monitoring?

- **Structured Logging**: Serilog with JSON output. Log events include correlation ID, request path, user ID, duration, and custom properties. Console sink for development; in production, add Seq, Application Insights, or ELK.

- **Correlation IDs**: Middleware extracts/generates `X-Correlation-Id` header and pushes it to `LogContext`. Propagated from API → RabbitMQ message headers → Worker.

- **Health Checks**: `/healthz` (liveness) and `/readiness` (includes DB connectivity). Used by Docker/Kubernetes orchestrators.

- **Metrics** (recommended): OpenTelemetry for request duration, error rate, queue depth. Export to Prometheus/Grafana.

### 8. Explain your approach to API versioning.

I prefer **URL path versioning** (e.g., `/api/v1/orders`) for clarity, though header-based versioning (`api-version` header) is also viable.

Strategy:
1. Start with `/api/` (implicit v1) — keeps URLs clean for single-version APIs.
2. When breaking changes are needed, introduce `/api/v2/` while maintaining `/api/v1/`.
3. Use `Asp.Versioning.Mvc` NuGet package for formal versioning with `[ApiVersion]` attributes.
4. Deprecation headers (`Sunset`, `Deprecation`) to signal upcoming removals.
5. OpenAPI spec per version.

### 9. How do you optimize database queries in EF Core?

1. **AsNoTracking**: For read-only queries (all GET endpoints) — avoids change tracker overhead.
2. **Projection**: Select only needed columns via `.Select()` instead of loading full entities.
3. **Composite Indexes**: `IX_Orders_CustomerId_Status_CreatedAt` covers common filter + sort patterns.
4. **Eager Loading**: `.Include(o => o.LineItems)` to prevent N+1 queries.
5. **Pagination**: Server-side with `.Skip().Take()`, enforced `pageSize ≤ 100`.
6. **Compiled Queries**: For hot-path queries called thousands of times per second.
7. **Split Queries**: `.AsSplitQuery()` for complex includes to avoid cartesian explosion.
8. **Query Logging**: EF Core query logging in development to catch unexpected query patterns.

### 10. Describe your message queue implementation approach.

I use **RabbitMQ** with the raw `RabbitMQ.Client` v7 library for full control:

- **Outbox Pattern**: Messages are persisted in the database first, then published by a background service with publisher confirms enabled.
- **Publisher Confirms**: Ensures messages are durably written to RabbitMQ before marking them as processed in the outbox.
- **Consumer**: Dedicated Worker service with `BasicConsume`. Consumer ACK after successful processing.
- **Dead Letter Queue (DLQ)**: Messages that fail after max retries are routed to a DLQ for investigation.
- **Idempotent Processing**: Consumer checks order version/event ID to skip already-processed messages.
- **Retry**: Exponential backoff with jitter on transient failures.

### 11. How do you ensure code quality in a team environment?

1. **CI Pipeline**: Automated build → test → lint → Docker build on every PR.
2. **Code Review**: Required before merge; focus on architecture, edge cases, naming.
3. **Static Analysis**: Roslyn analyzers, `dotnet format`, ESLint for frontend.
4. **Testing Standards**: Minimum coverage thresholds; new features require tests.
5. **Architecture Decision Records (ADRs)**: Document key decisions (ORM choice, auth strategy).
6. **Consistent Style**: `.editorconfig`, Prettier, ESLint enforce formatting.
7. **Branch Strategy**: Trunk-based with short-lived feature branches.

---

## Section B: SQL Questions

### 1. Write a query to find all customers who have placed more than 5 orders in the last 30 days.

```sql
SELECT c.Id, c.Name, c.Email, COUNT(o.Id) AS OrderCount
FROM Customers c
INNER JOIN Orders o ON o.CustomerId = c.Id
WHERE o.CreatedAtUtc >= DATEADD(DAY, -30, GETUTCDATE())
GROUP BY c.Id, c.Name, c.Email
HAVING COUNT(o.Id) > 5
ORDER BY OrderCount DESC;
```

### 2. Write a query to calculate the total order value per SADC country, converted to ZAR.

```sql
-- Assumes an FxRates table with columns: FromCurrency, ToCurrency, Rate
SELECT 
    c.CountryCode,
    o.CurrencyCode,
    COUNT(o.Id) AS OrderCount,
    SUM(o.TotalAmount) AS OriginalTotal,
    ROUND(SUM(o.TotalAmount * COALESCE(fx.Rate, 1)), 2) AS TotalInZar
FROM Orders o
INNER JOIN Customers c ON c.Id = o.CustomerId
LEFT JOIN FxRates fx ON fx.FromCurrency = o.CurrencyCode AND fx.ToCurrency = 'ZAR'
GROUP BY c.CountryCode, o.CurrencyCode
ORDER BY c.CountryCode;
```

### 3. Write a query to identify the top 10 products by revenue across all SADC countries.

```sql
SELECT TOP 10
    li.ProductSku,
    COUNT(DISTINCT o.Id) AS OrderCount,
    SUM(li.Quantity) AS TotalQuantity,
    SUM(li.Quantity * li.UnitPrice) AS TotalRevenue
FROM OrderLineItems li
INNER JOIN Orders o ON o.Id = li.OrderId
INNER JOIN Customers c ON c.Id = o.CustomerId
WHERE o.Status <> 4  -- Exclude cancelled orders (OrderStatus.Cancelled)
GROUP BY li.ProductSku
ORDER BY TotalRevenue DESC;
```

### 4. Write a query to detect potential duplicate orders (same customer, same items, within 5 minutes).

```sql
WITH OrderFingerprint AS (
    SELECT 
        o.Id AS OrderId,
        o.CustomerId,
        o.CreatedAtUtc,
        STRING_AGG(CONCAT(li.ProductSku, ':', li.Quantity), ',') 
            WITHIN GROUP (ORDER BY li.ProductSku) AS ItemsHash
    FROM Orders o
    INNER JOIN OrderLineItems li ON li.OrderId = o.Id
    GROUP BY o.Id, o.CustomerId, o.CreatedAtUtc
)
SELECT 
    a.OrderId AS Order1,
    b.OrderId AS Order2,
    a.CustomerId,
    a.ItemsHash,
    a.CreatedAtUtc AS Order1Time,
    b.CreatedAtUtc AS Order2Time,
    DATEDIFF(SECOND, a.CreatedAtUtc, b.CreatedAtUtc) AS SecondsBetween
FROM OrderFingerprint a
INNER JOIN OrderFingerprint b 
    ON a.CustomerId = b.CustomerId
    AND a.ItemsHash = b.ItemsHash
    AND a.OrderId < b.OrderId
    AND ABS(DATEDIFF(SECOND, a.CreatedAtUtc, b.CreatedAtUtc)) <= 300;
```

### 5. Write a query to generate an order processing time report (time from Pending to Fulfilled).

```sql
-- Assumes order status changes are tracked via UpdatedAtUtc 
-- or a separate OrderStatusHistory table for precise timing.
-- Simplified version using Order timestamps:
SELECT 
    o.Id,
    o.CustomerId,
    o.CreatedAtUtc AS OrderPlaced,
    o.UpdatedAtUtc AS LastStatusChange,
    o.Status,
    DATEDIFF(MINUTE, o.CreatedAtUtc, o.UpdatedAtUtc) AS ProcessingMinutes
FROM Orders o
WHERE o.Status = 3  -- Fulfilled
ORDER BY ProcessingMinutes DESC;
```

For a more accurate report, an `OrderStatusHistory` audit table would be recommended to capture the exact timestamp of each transition.

### 6. Explain the difference between `INNER JOIN`, `LEFT JOIN`, and `CROSS JOIN`.

| Join Type | Behavior | Use Case |
|-----------|----------|----------|
| `INNER JOIN` | Returns only rows with matches in **both** tables | Orders with customers (exclude orphan orders) |
| `LEFT JOIN` | Returns **all** rows from the left table; NULLs where no match in right | All customers, even those without orders |
| `CROSS JOIN` | Cartesian product — every row × every row | Generate combinations (e.g., products × regions) |

Example: To find customers without orders:
```sql
SELECT c.Id, c.Name
FROM Customers c
LEFT JOIN Orders o ON o.CustomerId = c.Id
WHERE o.Id IS NULL;
```

### 7. What are indexes and when would you use them?

**Indexes** are data structures (typically B-tree in SQL Server) that speed up data retrieval at the cost of additional storage and slower writes.

**When to use:**
- Columns frequently used in `WHERE`, `JOIN`, `ORDER BY`, `GROUP BY` clauses
- Composite indexes for multi-column filter patterns
- Filtered indexes for partial data sets (e.g., unprocessed outbox messages)
- Covering indexes to avoid key lookups

**In this project:**
- `IX_Orders_CustomerId_Status_CreatedAt` — composite index covering the most common query pattern
- Filtered index on `OutboxMessages` where `ProcessedAtUtc IS NULL` — only indexes unprocessed rows

**When NOT to use:**
- Low-cardinality columns (e.g., boolean flags on small tables)
- Tables with heavy write throughput and rare reads
- When the table is small enough for full scans to be faster

### 8. Explain the concept of database normalization (1NF through 3NF).

| Normal Form | Rule | Example Violation |
|-------------|------|-------------------|
| **1NF** | Atomic values, no repeating groups | Storing multiple phone numbers in one column: `"123,456"` |
| **2NF** | 1NF + no partial dependencies (non-key column depends on part of composite key) | In a `(OrderId, ProductSku) → ProductName` table, `ProductName` depends only on `ProductSku` |
| **3NF** | 2NF + no transitive dependencies (non-key column depends on another non-key column) | Storing `CustomerName` in the Orders table (depends on `CustomerId`, not `OrderId`) |

This project's schema is in **3NF**:
- Customer data is in the `Customers` table only (not duplicated in Orders)
- Line items are in a separate table with their own PK
- No computed values are stored (TotalAmount is computed but stored as a precomputed summary for query performance — a controlled denormalization)

### 9. How would you optimize a slow-running query?

Step-by-step approach:

1. **Identify**: Use SQL Server's execution plan (`SET STATISTICS IO ON`, `INCLUDE ACTUAL EXECUTION PLAN`) to find bottlenecks.
2. **Indexes**: Check for missing index suggestions in the execution plan. Add covering indexes.
3. **Query rewrite**: Replace correlated subqueries with JOINs or CTEs. Avoid `SELECT *`.
4. **Statistics**: Ensure statistics are up to date (`UPDATE STATISTICS`). Stale statistics → bad plans.
5. **Parameter sniffing**: Use `OPTION (RECOMPILE)` or `OPTIMIZE FOR` for queries with wildly varying parameter distributions.
6. **Pagination**: Replace `OFFSET/FETCH` with keyset pagination for large offsets.
7. **Partitioning**: For very large tables, consider table partitioning by date.
8. **Caching**: Add application-level caching for expensive, infrequently-changing queries.

### 10. Write a query to implement pagination with total count.

```sql
-- Keyset pagination (more performant than OFFSET for large datasets)
DECLARE @PageSize INT = 20;
DECLARE @LastId UNIQUEIDENTIFIER = NULL;  -- NULL for first page

SELECT 
    o.Id, o.CustomerId, o.Status, o.CurrencyCode, o.TotalAmount, o.CreatedAtUtc
FROM Orders o
WHERE (@LastId IS NULL OR o.Id > @LastId)
ORDER BY o.Id
OFFSET 0 ROWS FETCH NEXT @PageSize ROWS ONLY;

-- OFFSET/FETCH pagination with total count (used in this project via EF Core)
DECLARE @Page INT = 1;
DECLARE @PageSize2 INT = 20;

SELECT 
    o.Id, o.CustomerId, o.Status, o.CurrencyCode, o.TotalAmount, o.CreatedAtUtc,
    COUNT(*) OVER() AS TotalCount
FROM Orders o
ORDER BY o.CreatedAtUtc DESC
OFFSET (@Page - 1) * @PageSize2 ROWS
FETCH NEXT @PageSize2 ROWS ONLY;
```

In EF Core, this is implemented via the `PaginatedList<T>` helper:
```csharp
var totalCount = await query.CountAsync();
var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
```
