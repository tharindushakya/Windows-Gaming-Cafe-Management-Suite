# Production Readiness Report — Windows Gaming Café Management Suite

Date: 2025-09-12

This report captures the current production readiness of the solution across API, Admin UI, POS, data, security, reliability, and operations based on repository inspection and a local build/test run.

## Executive summary

- Overall: Strong foundation with authentication/authorization, API versioning, health checks, rate limiting, and logging already in place.
- Key gaps to resolve before production:
  - Target .NET 8 LTS instead of .NET 10 preview across all projects to avoid preview runtime risk.
  - CI: stabilize tests and avoid file-lock failures by not running the API while testing; fix 1 failing UI test.
  - Secrets: move all secrets (JWT, DB, SMTP) out of appsettings; use env vars or Key Vault.
  - Swagger: disable UI in production or protect behind auth.
  - Containerization and deployment IaC are missing; add Dockerfiles and Bicep/Terraform or azd.
  - Data protection keys and distributed cache: configure Redis-backed DP keys for multi-instance deployments.

## Architecture snapshot

- Solution: API (ASP.NET Core), Admin (React), POS (WPF), Core/Application/Data layers, tests.
- API features observed in `Program.cs`:
  - API Versioning + Swagger (v1)
  - JWT authentication; policy-based authorization
  - Health checks (`/health`, `/health/live`) with DB, email, redis, backups, file upload checks
  - Rate limiting (global + named policies), custom auth rate limit middleware
  - Serilog logging (console, rolling file), structured logging
  - Hangfire background jobs (prefers PostgreSQL, falls back to in-memory)
  - CORS policy “LocalhostOnly”; HSTS/HTTPS redirection in non-development
  - Basic metrics endpoint comment (Prometheus-style) enabled later in pipeline
  - Redis health check present; cache service registered

## Build, test, and quality gates

- Build: Succeeds with `dotnet build` (while API not locking binaries)
- Tests: `dotnet test` yielded:
  - 1 failing test: `GamingCafe.UI.Tests.NavMenuTests.Shows_Protected_Links_When_Authenticated` (UI content expectations)
  - Build errors due to file locks (API running during tests) — fix by stopping API before tests or isolating outputs

Actions

- Switch all projects to .NET 8 LTS target frameworks and SDK.
- Update CI to ensure no background API process holds locks during `dotnet test`.
- Fix the failing UI test or align the menu rendering to test expectations.

## Security and secrets management

Findings

- `appsettings.json` contains placeholders for sensitive values (JWT key, DB, SMTP). `appsettings.Development.json` contains a development JWT key and local connection strings.
- JWT validation configured; throws if key missing.
- Authorization policies defined; custom permission handlers registered.

Actions

- Store all secrets in environment variables or Azure Key Vault; remove secrets from repo.
- Rotate the development JWT key; use short-lived secrets in dev.
- Enable HTTPS-only cookies for auth; verify SameSite/Lax/Strict as appropriate.
- Consider adding security headers middleware (e.g., Content-Security-Policy, X-Content-Type-Options, Referrer-Policy).
- Periodically run dependency and code scanning (CodeQL already present).

## Networking, TLS, and CORS

Findings

- HSTS and HTTPS redirection are enabled in non-Development.
- CORS policy “LocalhostOnly” is configured for dev.

Actions

- For production, define explicit allowed origins (admin domain, POS local network if applicable). Avoid `*`.
- Terminate TLS at the load balancer/reverse-proxy with modern ciphers; ensure backend also enforces HTTPS.

## Observability: logging, metrics, tracing

Findings

- Serilog configured for console and rolling files with retention; context enrichment enabled.
- Health checks mapped with detailed JSON output; tags used per check.
- Commented OpenTelemetry note; metrics endpoint exposed.

Actions

- Decide final metrics approach: either enable OpenTelemetry Metrics/Traces or keep slim Prometheus metrics; wire to Azure Monitor/Grafana.
- Centralize logs (e.g., to Seq/Elastic/Azure Monitor). Avoid local file-only logging in production.
- Scrub PII from logs; add structured properties for correlation (userId, requestId).

## Reliability, performance, and rate limiting

Findings

- Rate limiter configured: global fixed window + token bucket for auth routes; custom auth limiter middleware present.
- HTTP client policies via Polly: timeout, retries with jitter, circuit breaker.
- Cache service and Redis health check present; product listing was updated to make cache keys parameter-aware.

Actions

- Use Redis for distributed cache and response caching across instances.
- Validate limiter thresholds and overrides; align to expected RPS.
- Add output/response caching for read-heavy endpoints where safe.
- Add k6/load tests (k6 folder present) for critical flows; expand beyond login.

## Background processing (Hangfire) and outbox

Findings

- Hangfire configured with Postgres when available; otherwise in-memory (not suitable for production).
- Hangfire dashboard exposed (default `/hangfire`), with a dashboard auth filter.
- Outbox dispatcher wired for Hangfire.

Actions

- Provision a persistent Hangfire database/schema; remove in-memory fallback in production.
- Protect dashboard with strong auth and IP restrictions or disable in production.
- Set explicit Hangfire worker counts and queues to match workload.

## Data layer, migrations, and backups

Findings

- EF Core migrations present; DB retry on failure enabled.
- Backup services and health check present.

Actions

- Adopt a controlled migration step in CI/CD (no auto-migrate on startup in production unless planned), with backup/restore validation.
- Verify backup schedule, retention, and restore runbooks.

## File storage and uploads

Findings

- File upload health check present; local uploads directory exists.

Actions

- Move file storage to durable object storage (e.g., Azure Blob Storage) with signed URLs and virus scanning.
- Enforce upload size/type limits and validation.

## API hygiene and compatibility

Findings

- API versioning configured; Swagger doc for v1. Some code also registers a generic `v1` doc directly.

Actions

- Keep Swagger UI disabled or protected in production; enable per environment with auth.
- Adopt consistent versioning strategy and deprecation policy.

## Frontend (Admin) readiness

Findings

- React app compiles; search UX unified and fixed; empty-states updated.
- Paged lists component generalized for empty states.

Actions

- Produce a production build and serve via a CDN or from the API/static hosting; add cache headers.
- Add e2e smoke tests for critical flows (auth, search, transactions, inventory, reservations).

## POS (WPF) readiness

Findings

- Targets `net10.0-windows7.0` (preview). Packaging strategy not defined.

Actions

- Retarget to net8.0-windows and set up MSIX or installer packaging; define update strategy.

## CI/CD and infrastructure

Findings

- GitHub Actions present (CI, CodeQL, package governance). No deployment pipeline, Dockerfiles, or IaC.
- API contains `infra/` scripts for Hangfire Postgres schema only.

Actions

- Add Dockerfiles for API and Admin; optionally Nginx for static + reverse proxy.
- Create Bicep/Terraform (or azd) for App Service/Container Apps, Postgres, Redis, Key Vault, Storage, Log Analytics.
- Add environment-specific appsettings via secrets and `DOTNET_` env vars; never commit secrets.
- Introduce CD workflow: build, test, containerize, push, deploy with blue/green or slot swaps.

## Configuration checklist (production)

- Runtime
  - [ ] Target .NET 8 LTS (all projects), pin SDK in `global.json`
  - [ ] Disable Swagger UI (or protect) in prod
  - [ ] Enable HSTS/HTTPS in prod (already present)
- Secrets and config
  - [ ] JWT key from Key Vault/env; rotate and monitor
  - [ ] DB/Redis/SMTP from Key Vault/env; encrypted at rest/in transit
  - [ ] Data Protection keys persisted in Redis/Storage; shared across instances
- Security
  - [ ] Restrictive CORS; no wildcard
  - [ ] Security headers middleware enabled
  - [ ] Rate limits tuned; WAF rules configured at edge
  - [ ] Admin endpoints protected and audited
- Observability
  - [ ] Centralized logs, metrics, traces; dashboards and alerts defined
  - [ ] Health endpoints integrated with LB/K8s probes
- Performance
  - [ ] Caching (Redis) for hot reads; cache keys parameter-aware
  - [ ] Load tests for peak scenarios (login, transactions, inventory)
- Data and jobs
  - [ ] Controlled migrations step with pre-backup
  - [ ] Hangfire on Postgres; dashboard locked down
  - [ ] Backups tested; RTO/RPO documented
- Frontend and POS
  - [ ] Admin prod build with CDN caching
  - [ ] POS packaging and update strategy
- Compliance
  - [ ] PII minimization in logs; retention policies
  - [ ] License and third-party notices

## Known issues and risks (as of this report)

- Using .NET 10 preview — upgrade to .NET 8 LTS for stability and support window.
- One UI test failing; investigate changes to nav menu or test expectations.
- Tests failed due to file locks when API is running; ensure isolated build outputs and stop background services in CI.
- Swagger UI enabled unconditionally — restrict in production.
- Secrets in appsettings (placeholders/dev) — ensure secrets are injected from secure stores.

## Suggested next steps (1–2 sprints)

Sprint 1

- Retarget frameworks to net8.0; verify build/test; fix failing UI test.
- Add Dockerfiles for API and Admin; compose for local.
- Add production appsettings templates and Key Vault wiring.
- Lock down Swagger UI and add security headers.

Sprint 2

- Introduce IaC (Bicep/azd) for App Service/Container Apps, Postgres, Redis, Key Vault, Log Analytics.
- Configure Data Protection keys in Redis; switch cache provider to Redis for multi-instance.
- Add CI/CD deploy workflow with blue/green deploys and migration step.
- Expand k6 tests to cover transactions/inventory and set SLOs/alerts.

---

This document supersedes any prior “PRODUCTION_READINESS.md”.

## Production Readiness & Architecture Deep-Dive

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
