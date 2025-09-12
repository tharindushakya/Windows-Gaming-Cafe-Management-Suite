# Production Readiness & Architecture Deep-Dive

This document provides a comprehensive audit of the current codebase and a prioritized roadmap to make the Windows Gaming Café Management Suite production-ready.

## 1. Overview

A multi-project .NET 8 solution (API, Blazor Admin, WPF POS, Core, Data, Tests) already implements core domain concerns: authentication (JWT + refresh), 2FA, sessions, inventory, loyalty, auditing, background jobs (Hangfire), caching (Redis), and real-time (SignalR). Several production-critical aspects need refinement (security hardening, observability, scaling, consistency, resilience, structured governance).

## 2. Strengths

- Clear project separation (Core/Data/API/UI)
- Explicit EF Core model configuration (indices, conversions, precision)
- JWT + refresh token + 2FA workflow scaffolded
- Hangfire with PostgreSQL + in‑memory fallback logic
- Health checks with tags
- Redis integration (graceful fallback attempt)
- Audit logging concept present
- Metrics endpoint (basic) provided

## 3. High-Risk / High-Impact Gaps

| # | Gap | Impact | Priority |
|---|-----|--------|----------|
| 1 | `app.UseRateLimiter()` without `AddRateLimiter()` registration | Runtime failure / no protection | 🔴 P0 |
| 2 | Duplicate Swagger registrations | Confusion / config drift | 🟠 P1 |
| 3 | Plain-text single refresh token field on User | Token theft risk, no device revocation | 🔴 P0 |
| 4 | `Task.Run` for async side-effects in requests | Lost errors, thread starvation | 🔴 P0 |
| 5 | In-memory 2FA transient token cache | Fails in multi-instance scenario | 🔴 P0 |
| 6 | Local file-system DataProtection keys | Non-sticky scaling breaks auth | 🔴 P0 |
| 7 | Old SignalR package (`1.1.0`) | Incompatibility / missing features | 🔴 P0 |
| 8 | Serilog referenced but disabled | Missing structured diagnostics | 🟠 P1 |
| 9 | EnsureCreated + Migrate combo | Migration race / drift risk | 🟠 P1 |
|10 | No policy-based authorization | Coarse security model | 🟠 P1 |
|11 | Manual null checks vs FluentValidation | Inconsistent validation | 🟡 P2 |
|12 | Tokens/Secrets likely in appsettings | Secret leakage risk | 🔴 P0 |
|13 | No concurrency control on monetary ops | Double-spend / data races | 🔴 P0 |
|14 | No retry/timeouts (Polly) for infra | Cascading failures | 🟠 P1 |
|15 | Missing structured metrics/tracing (OTel commented) | Low observability | 🟠 P1 |
|16 | Single refresh token – no reuse detection | Silent theft exploitation | 🟠 P1 |
|17 | Manual /metrics instead of exporter | Reinventing wheel | 🟡 P2 |
|18 | Disparate provider choice (Postgres vs SqlServer) | Operational complexity | 🟡 P2 |
|19 | No automated test coverage for critical paths | Regression risk | 🔴 P0 |
|20 | No CI/CD or package governance | Drift & vulnerability exposure | 🟠 P1 |

## 4. Target Architecture Evolution

```text
+-----------------------------+        +--------------------------+
|  React Admin (Server)      |  -->   |  API (ASP.NET Core)      |
+-----------------------------+        |  Auth / Sessions / Jobs  |
                                        |  Domain Orchestration    |
+-----------------------------+        +--------------------------+
|  WPF POS Client             |  -->   |  Application Layer*      |
+-----------------------------+         (Commands / Queries /     )
                                         (Domain Events / Policies)
                          +---------------------------------------+
                          | Core Domain (Entities, Value Objects) |
                          +---------------------------------------+
                          | Data (EF Core + Outbox + Migrations)  |
                          +------------------+--------------------+
                          | Infrastructure   | Cross-Cutting      |
                          |  (Email, Redis,  |  (Logging, OTel,   |
                          |   Hangfire,      |   Caching, Sec)    |
                          +------------------+--------------------+
```
*Application layer optional initially; can be introduced incrementally.

## 5. Domain & Data Layer Recommendations

- Add value objects (Money, EmailAddress, Rate, StationIdentifier)
- Apply optimistic concurrency tokens (`[Timestamp]`) to User (wallet), Inventory, Transactions
- Introduce Outbox table (Id, AggregateId, Type, Payload, Status, AttemptCount, LastAttemptAt)
- Remove `EnsureCreated`; rely solely on migrations
- Add EF Core interceptors: Audit, SoftDelete (if required), Concurrency logging

## 6. Security Hardening

| Area | Action |
|------|--------|
| Refresh Tokens | Move to separate table (UserRefreshToken) with hashed token, device info, expiry, revoked flag |
| Token Reuse Detection | On refresh: mark old token as used; deny if used again |
| 2FA Flow | Store transient token & state in Redis (namespace: `auth:2fa:`) |
| Password Policies | Enforce length, complexity, breach check (optional HIBP) |
| Secrets | Use environment variables / Secret Manager (dev) / Key Vault (prod) |
| DataProtection | Persist keys to Redis or DB for multi-node |
| Headers | Add CSP, Permissions-Policy, Remove deprecated X-XSS-Protection (modern browsers), keep HSTS in prod |
| Role & Policy | Add policies: `ManageInventory`, `ManageStations`, `ViewFinancials`, etc. |
| Anti-Automation | Rate limit login, password reset, email verification separately |
| Sensitive Logging | Never log raw tokens or PII; mask email/phone in audit events |

## 7. Observability

- Reinstate OpenTelemetry: Traces (AspNetCore, HttpClient, EF), Metrics (runtime + domain), Logs (Serilog bridge)
- Export via OTLP → Prometheus / Jaeger / Tempo
- Correlation ID middleware; enrich logs with TraceId & SpanId
- Replace manual `/metrics` with OTel Prometheus exporter
- Define custom metrics: `sessions_active`, `wallet_transactions_total`, `inventory_low_count`, `rate_limit_rejected_total`

## 8. Performance & Resilience

- Introduce Polly policies (retry w/ jitter, timeout, circuit breaker) for: Redis, SMTP, External Payment (future)
- Apply `.AsNoTracking()` on read-only queries
- Add paging defaults (e.g., max 100) with continuation tokens or page+size
- Precompute heavy dashboard aggregates via Hangfire recurring jobs
- Warmup: optional startup job compiling expression trees for hot paths

## 9. API Design & Versioning

- Refactor routes to include version segment: `/api/v{version}/auth`
- Add `[ApiVersion("1.0")]` on controllers
- Single `AddSwaggerGen` with versioned docs using `IApiVersionDescriptionProvider`
- Use RFC7807 ProblemDetails consistently (already partly applied)
- Add global exception handler returning ProblemDetails with correlation id

## 10. Validation Strategy

- FluentValidation for all request DTOs (Login, Register, 2FA, Reservation, Product, Transaction)
- Remove inline null checks once validators implemented
- Normalize (trim & lower-case) emails/usernames in validators

## 11. Caching Strategy

| Cache Item | Store | TTL | Notes |
|------------|-------|-----|-------|
| 2FA transient tokens | Redis | 5 min | Key pattern `auth:2fa:{token}` |
| Station status snapshot | Redis | 10–30 sec | Real-time UI sync |
| Product catalog | Memory (with version) | 5 min | Bust on inventory change |
| Loyalty program config | Memory/Redis | 1 hr | Rarely changes |

## 12. Background Processing & Async Patterns

- Replace `Task.Run` with Hangfire `BackgroundJob.Enqueue`
- Use Outbox dispatcher background service (poll unprocessed events → schedule Hangfire job)
- Add recurring jobs: Cleanup expired refresh tokens, Purge old audit logs, Recalculate KPI aggregates

## 13. Authorization & Policies

Example policies:

- `RequireRole("Admin")`
- `ManageInventory` (Admin or Staff with claim `inv:write`)
- `ViewFinancials` (Admin only)
- `IssueRefunds` (Admin + claim `txn:refund`)
Add a `Permission` claim type to avoid hard-coding roles in logic.

## 14. Financial / Wallet Integrity

- Add WalletTransactions ledger (Id, UserId, Type, Amount(+/-), BalanceAfter, CorrelationId, Timestamp)
- All wallet mutations executed in serializable or repeatable read transaction + check optimistic concurrency token
- Idempotency: Accept `Idempotency-Key` header for wallet-affecting POST endpoints; store processed keys

## 15. Audit Logging Enhancements

- Interceptor: capture entity changes (Limited size; truncate large payloads)
- Enrich with RequestId, CorrelationId, UserId, RemoteIP, UserAgent
- Provide filtering & pagination endpoints (indexing already present on `Timestamp` + `(EntityType, EntityId)`)

## 16. Package & Dependency Governance

Steps:

1. Add `Directory.Packages.props` to centralize versions
2. Run `dotnet list package --outdated --include-transitive`
3. Upgrade in waves (framework-aligned first)
4. Remove obsolete `Microsoft.AspNetCore.SignalR` package (use built-in)
5. Add CI job failing build on vulnerable packages (`--vulnerable`)
6. Dependabot for NuGet

## 17. Testing Strategy

| Layer | Tests |
|-------|-------|
| Unit | AuthService (2FA branches), Token refresh lifecycle, TwoFactorService, BackupService, RateLimiter metrics, Email token generation |
| Integration | Auth workflow, Reservation lifecycle, Inventory movement, Hangfire scheduling, Concurrency conflict on wallet |
| Contract | Snapshot Swagger JSON diff (breaking changes) |
| Performance (baseline) | Login & Start Session endpoints under load (Locust/JMeter) |
| Security | JWT tamper test, Replay refresh token detection |
| UI (Blazor) | bUnit for core components (Station list virtualization, Session panel) |

## 18. CI/CD & DevOps

- GitHub Actions: build + test + format + vuln scan
- Optional artifact publish (API container, Blazor server, WPF MSI via `dotnet publish` + WiX later)
- Add Dockerfiles + `docker-compose.yml` (API, Admin, Postgres, Redis, Mailhog)
- Infra as Code (Bicep/Terraform) if/when cloud target introduced (Key Vault, App Service / Container Apps, Azure Database for PostgreSQL, Redis Cache)

## 19. Configuration & Options Binding

- Strongly-typed options: JwtOptions, RateLimitingOptions, EmailOptions, BackupOptions
- Validate at startup (`ValidateOnStart`) + DataAnnotations
- Environment variable overrides for secrets

## 20. Rate Limiting Proper Registration (Planned Code Snippet)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});
```

Add per-user partition for authenticated calls (claims-based key) and a stricter partition for login endpoints.

## 21. Phased Execution Roadmap
 
### Phase 0 (Immediate)

- AddRateLimiter, remove duplicate Swagger
- Enable Serilog (Console JSON + rolling file)
- Remove EnsureCreated, keep Migrate
- Hash & externalize refresh tokens
- Replace Task.Run with Hangfire jobs

### Phase 1

- FluentValidation coverage
- Concurrency tokens & wallet ledger scaffold
- Redis for 2FA transient state
- Directory.Packages.props, version alignment
- CI build/test + dependabot

### Phase 2

- OpenTelemetry integration (traces + metrics + logs instrumentation)
- Policy-based authorization & permission claims
- Outbox + domain events + dispatcher service
- Structured audit interceptor

### Phase 3

- Containerization + docker-compose
- Security headers + CSP & secret management
- Idempotent wallet & transaction APIs
- Performance/load testing baseline

### Phase 4

- Advanced reporting (precomputed aggregates)
- Multi-instance scaling (shared DataProtection keys)
- Soft delete & retention policies (if business requires)
- Multi-location tenancy model (schema or discriminator strategy)

## 22. Edge Cases to Test

- Simultaneous wallet debit attempts (ensure optimistic concurrency fail & retry)
- Refresh token reuse after rotation (detect + revoke chain)
- Expired 2FA token consumption attempt
- Redis outage fallback path (graceful degradation)
- Hangfire Postgres schema missing (already partially handled)
- JWT with modified signature (should reject) / expired refresh path

## 23. Suggested New Tables (DDL Sketch)

```sql
CREATE TABLE UserRefreshTokens (
  Id BIGSERIAL PRIMARY KEY,
  UserId INT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
  HashedToken TEXT NOT NULL,
  DeviceLabel TEXT NULL,
  CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  ExpiresAt TIMESTAMPTZ NOT NULL,
  RevokedAt TIMESTAMPTZ NULL,
  ReplacedByToken TEXT NULL,
  Used BIT NOT NULL DEFAULT 0
);

CREATE TABLE OutboxMessages (
  Id BIGSERIAL PRIMARY KEY,
  AggregateId TEXT NULL,
  Type TEXT NOT NULL,
  Payload JSONB NOT NULL,
  Status SMALLINT NOT NULL DEFAULT 0,
  AttemptCount INT NOT NULL DEFAULT 0,
  LastAttemptAt TIMESTAMPTZ NULL,
  CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE WalletTransactions (
  WalletTransactionId BIGSERIAL PRIMARY KEY,
  UserId INT NOT NULL REFERENCES Users(UserId) ON DELETE CASCADE,
  Type SMALLINT NOT NULL,
  Amount DECIMAL(18,2) NOT NULL,
  BalanceAfter DECIMAL(18,2) NOT NULL,
  CorrelationId UUID NOT NULL,
  Metadata JSONB NULL,
  CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (CorrelationId)
);
```

## 24. Optional Enhancements (Later)

- gRPC for internal station control
- WebSocket fallback mode for constrained networks
- Feature flags (e.g., LaunchDarkly or simple DB toggles) for gradual rollout
- Tiered pricing rules engine (scripting with C# Roslyn or embedded DSL)

## 25. Success Criteria Definition

| Category | KPI |
|----------|-----|
| Reliability | < 1% failed API requests (5xx) under nominal load |
| Security | Zero critical vulnerabilities (SNYK / GitHub Advisory scan) |
| Performance | P95 login < 300ms / start session < 500ms |
| Data Integrity | Zero double-spend wallet incidents in tests |
| Observability | 95% trace sampling coverage for critical flows |
| Test Coverage | 70%+ service layer, 90% of auth critical paths |

## 26. Immediate Action Summary

1. Implement & configure AddRateLimiter
2. Refactor Swagger registration to single block
3. Introduce refresh token table + hashing
4. Enable Serilog pipeline
5. Remove EnsureCreated usage
6. Migrate 2FA token handling to Redis

---
**Next Step:** Execute Phase 0 changes (on request). Let this document guide code modifications, reviews, and deployment hardening.
