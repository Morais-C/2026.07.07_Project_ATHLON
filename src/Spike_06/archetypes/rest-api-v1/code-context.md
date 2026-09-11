# rest-api-v1 — CodeContext rules

## Fixture baseline

| Property | Value |
|----------|-------|
| Fixture id | `mini-erp-v1` |
| Path | `../../fixtures/mini-erp-v1` (relative to pack root) |
| Entry project | `MiniErp/MiniErp.csproj` |
| Contract test project | `MiniErp.ContractTests/MiniErp.ContractTests.csproj` |

## What gets loaded

The CodeContextBuilder loads all files under the fixture matching the configured extensions, excluding `bin/` and `obj/` directories:

| Extension | Purpose |
|-----------|---------|
| `.cs` | API implementation (`Program.cs`) and contract tests |
| `.csproj` | Project files for build configuration |
| `.yaml` / `.yml` | OpenAPI specification (`openapi.yaml`) |
| `.json` | Configuration files (e.g., `appsettings.json` if present) |

## Baseline file structure

```text
mini-erp-v1/
  openapi.yaml              # OpenAPI contract (source of truth)
  MiniErp/
    Program.cs              # Minimal API endpoints
    MiniErp.csproj          # API project
  MiniErp.ContractTests/
    HealthEndpointTests.cs  # Contract tests (WebApplicationFactory + xUnit)
    MiniErp.ContractTests.csproj
```

## Caps (from archetype.json)

| Cap | Value |
|-----|-------|
| Max files loaded | 16 |
| Max total chars | 64,000 |

Over-cap fixture → CodeContextBuilder aborts before Planner runs.

## Content hashes

CodeContext artifact records per-file content hash (SHA-256). This enables:
- Detecting drift between baseline and CodeContext
- Validating that diffs apply against expected content
