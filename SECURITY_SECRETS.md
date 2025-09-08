Purpose

This short file explains how to manage secrets for this repository.

Recommended safe workflow

1. Never commit passwords, API keys, or private certificates into source control.
2. Use dev-time `dotnet user-secrets` for local development and set production secrets via environment variables or a secret manager.

Local development (dotnet user-secrets)

- Run `dotnet user-secrets init` in the project folder (one time).
- Use `dotnet user-secrets set "Jwt:Key" "<your-dev-jwt-key>"` and similar to populate secrets.

CI / Production

- Store secrets in your platform's secret manager (GitHub Actions secrets, Azure Key Vault, AWS Secrets Manager, etc.) and expose them as environment variables to your runtime.
- Example environment variables expected by this repository:
  - `Jwt__Key`
  - `ConnectionStrings__DefaultConnection`
  - `DataProtection__Certificate__PfxPassword`
  - `API_SMOKE_TEST_PASSWORD` (used only by the smoke test tool)

If you need help wiring Azure Key Vault or another secrets provider into the ASP.NET Core configuration, ask and I will generate the minimal wiring code.
