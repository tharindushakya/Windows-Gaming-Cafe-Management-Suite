# Admin Refresh Tokens: Implementation Roadmap

Purpose
- Capture a concise, actionable plan for enhancements to the admin refresh-token endpoints so work can be picked up later and implemented consistently.

Requested items (kept as a checklist)
- [ ] Add audit logging for admin revocations.
- [ ] Add pagination and filtering to the list endpoint.
- [ ] Expose admin endpoints in Swagger and include example requests/responses.
- [ ] Add unit and integration tests for auth flows (login, refresh/rotate, revoke).

Design notes and concrete steps

1) Audit logging for admin revocations
- Goal: record who (admin user) revoked which token, when, and why (optional reason). Keep logs in DB audit table and application logs.
- Implementation sketch:
  - Use existing `IAuditService` / `GamingCafe.Data.Services.AuditService` pipeline. Add a new method or reuse an `CreateAuditAsync` call with details.
  - In `RefreshTokensController.RevokeForUser` call audit service after successful revocation:
    - Action: `"AdminRevokeRefreshToken"`
    - SubjectId: revoked `TokenId`
    - TargetUserId: `userId`
    - ActorId: read from JWT claim (NameIdentifier)
    - Metadata: { reason, deviceInfo, ipAddress }
  - Persist to existing audit table (or create a lightweight `AdminActionLog` entity if preferred).
- Files to change:
  - `src/GamingCafe.API/Controllers/RefreshTokensController.cs` (add audit call and extract admin id from claims)
  - `src/GamingCafe.API/Services/` or `GamingCafe.Data/Services/AuditService.cs` (ensure method supports custom audit entries)
- Edge cases:
  - Missing admin claim -> log anonymous admin action with request origin IP
  - Failure to write audit should not block revocation: log locally and return success

2) Pagination and filtering for list endpoint
- Goal: avoid returning large token lists and support searching/filters for active/expired, device, IP, date ranges.
- API changes (GET):
  - Endpoint: `GET /api/admin/refresh-tokens/{userId}`
  - Query parameters:
    - `page` (int, default 1)
    - `pageSize` (int, default 25, max 200)
    - `activeOnly` (bool) -> tokens with `RevokedAt IS NULL AND ExpiresAt > now()`
    - `device` (string) -> case-insensitive contains
    - `ip` (string) -> exact or contains
    - `from` / `to` (datetime) -> filter by CreatedAt range
  - Response shape (paged):
    {
      "items": [ { TokenDto } ],
      "page": 1,
      "pageSize": 25,
      "total": 123
    }
- Implementation sketch:
  - Build an `IQueryable<RefreshToken>` from `GamingCafeContext.RefreshTokens` filtered by `UserId` and query params.
  - Compute `total = await query.CountAsync()` before Skip/Take.
  - Apply ordering (CreatedAt desc), Skip((page-1)*pageSize).Take(pageSize).
- Files to change:
  - `src/GamingCafe.API/Controllers/RefreshTokensController.cs` (replace existing list implementation)
  - Add `TokenDto` in `GamingCafe.Core.DTOs` if not already present
- Edge cases:
  - Validate `page` and `pageSize` to prevent negative or excessively large values

3) Swagger exposure and example requests
- Goal: make admin endpoints visible in Swagger (when running in Development) and provide example requests/responses so other developers can try them easily.
- Steps:
  - Add XML comments to controller methods and DTOs; enable XML comments in `Program.cs` Swagger config:
    - In `GamingCafe.API.csproj` enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
    - In `Program.cs`, configure `SwaggerGen` to include XML docs: `opts.IncludeXmlComments(xmlPath)`
  - Add `ProducesResponseType` attributes for common responses (200, 400, 401, 404)
  - Add example request/response in documentation markdown or include sample `curl` and `httpie` snippets in the XML comments or README.
- Example request (curl):
  ```bash
  # List tokens (first page)
  curl -H "Authorization: Bearer <ADMIN_JWT>" "http://localhost:5148/api/admin/refresh-tokens/9?page=1&pageSize=25&activeOnly=true"

  # Revoke a token
  curl -X POST -H "Authorization: Bearer <ADMIN_JWT>" -H "Content-Type: application/json" \
    -d '{ "TokenId": "<token-guid>" }' \
    http://localhost:5148/api/admin/refresh-tokens/9/revoke
  ```

4) Unit and integration tests for auth flows
- Goal: provide a test suite that covers Authenticate, RefreshToken rotation, and Revoke flows.
- Types of tests:
  - Unit tests for `AuthService` behavior: hashing logic, ComputeHash, token persistence logic (use in-memory DbContext or mocks)
  - Integration tests for end-to-end flows using `WebApplicationFactory<TEntryPoint>` (Microsoft.AspNetCore.Mvc.Testing)
- Test cases to add (suggested names):
  - AuthService_Authenticate_ReturnsTokens_WhenCredentialsValid
  - AuthService_RefreshToken_RotatesAndRevokesOldToken
  - AuthService_RevokeRefreshToken_MarksTokenRevoked
  - AuthEndpoints_EndToEnd_Login_Refresh_Revoke_ShouldBehaveCorrectly (integration)
- Files to add/update:
  - `src/GamingCafe.Tests/AuthServiceTests.cs` (unit tests)
  - `src/GamingCafe.Tests/Integration/AuthFlowTests.cs` (integration test using WebApplicationFactory)
- Implementation notes:
  - Use an in-memory Postgres alternative or testcontainer (recommended) for integration tests to be realistic; simpler: use the existing dev DB but make sure tests clean up after themselves.
  - Seed test users using `DatabaseSeeder` or in-test setup.

5) Small DTO / code snippets (reference)
- TokenDto (add to `GamingCafe.Core.DTOs`):
  ```csharp
  public class TokenDto {
      public Guid TokenId { get; set; }
      public int UserId { get; set; }
      public string? DeviceInfo { get; set; }
      public string? IpAddress { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime ExpiresAt { get; set; }
      public DateTime? RevokedAt { get; set; }
  }
  ```

- Paged response wrapper (re-usable):
  ```csharp
  public class PagedResult<T> {
      public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
      public int Page { get; set; }
      public int PageSize { get; set; }
      public long Total { get; set; }
  }
  ```

- Audit call example (inside controller after successful revoke):
  ```csharp
  var actorId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : (int?)null;
  await _auditService.CreateAsync(new AuditEntry{
      Action = "AdminRevokeRefreshToken",
      ActorId = actorId,
      TargetUserId = userId,
      Subject = token.TokenId.ToString(),
      Metadata = new Dictionary<string,string>{ ["DeviceInfo"] = token.DeviceInfo ?? "", ["IpAddress"] = token.IpAddress ?? "" }
  });
  ```

Implementation timeline & effort estimate (rough)
- 2-3 hours: Add audit call and basic unit test
- 2-4 hours: Add pagination/filtering + DTOs + Swagger XML comments
- 3-6 hours: Integration tests with WebApplicationFactory (or Testcontainers)

Acceptance criteria
- Admin revoke actions are auditable and recorded
- List endpoint supports pagination and filters and returns total counts
- Endpoints are visible in Swagger with example requests
- Tests cover happy-paths for login/refresh/revoke and assert DB state changes

Notes / considerations
- Keep audit writes non-blocking to the user experience; prefer fire-and-forget with robust error logging.
- For integration tests, prefer ephemeral databases (testcontainer or dedicated test DB) to avoid interfering with developer DB state.
- Consider background job to prune expired/revoked tokens older than X days.

---
Saved to: `docs/admin-refresh-tokens-roadmap.md`

If you want, I can: 
- Implement these changes now (I can create patches for the controller, add DTOs, wire up audit calls, add XML comments, and scaffold tests). 
- Or I can break the document into smaller PR-ready tasks and implement them one-by-one on request.

Which would you like next?
