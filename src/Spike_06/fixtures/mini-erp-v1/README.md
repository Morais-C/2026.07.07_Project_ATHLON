# Fixture: mini-erp-v1

Near-empty **ASP.NET Minimal API / net9** baseline for the `rest-api-v1` archetype pack (Spike_06).

## Surface

| Item | Detail |
|------|--------|
| API project | `MiniErp/MiniErp.csproj` |
| Endpoints | `GET /health` only |
| Contract | Checked-in `openapi.yaml` (source of truth; ChangeRequests may patch it) |
| Contract tests | `MiniErp.ContractTests` — `WebApplicationFactory` + xUnit |
| Store / auth | In-memory only when resources are added; **no auth**, **no EF/SQL** |

## Intent

Demos add ERP resources via ChangeRequest (e.g. product, customer) by patching OpenAPI + Minimal API + contract tests together.

## Verify baseline

```powershell
dotnet build MiniErp/MiniErp.csproj
dotnet test MiniErp.ContractTests/MiniErp.ContractTests.csproj
```
