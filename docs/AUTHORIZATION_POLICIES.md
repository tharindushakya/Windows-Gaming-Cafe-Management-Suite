# Authorization policies

This project registers named authorization policies in `Program.cs` to provide a policy-based authorization model rather than relying purely on coarse role checks.

Default policies registered:

- `RequireAdmin` - requires role `Admin`
- `RequireManagerOrAdmin` - allows `Manager` or `Admin`
- `RequireStationScope` - requires claim `scope` with value `stations.manage`

How to use:

1. On controllers or actions:

```csharp
[Authorize(Policy = "RequireAdmin")]
public class AdminController : ControllerBase { }
```

1. On minimal API endpoints:

```csharp
app.MapPost("/admin/task", () => Results.Ok())
   .RequireAuthorization("RequireAdmin");
```

1. Apply policies to SignalR hubs via Hub filters or within hub methods using `Context.User`.

Recommendation: Review controller-level `[Authorize]` attributes in the codebase and replace coarse checks like `User.IsInRole("Admin")` scattered across methods with a centralized, testable policy.
