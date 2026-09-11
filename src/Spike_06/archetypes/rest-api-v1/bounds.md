# rest-api-v1 — Bounds checklist

Analyst + Planner + Coder rejection rules. Violation → **fail fast** without publishing downstream artifacts.

## In scope (ALL must be true)

| Rule | Detail |
|------|--------|
| Target runtime | Single existing **.NET 9 ASP.NET Minimal API** (baseline fixture `mini-erp-v1`) |
| App pattern | REST resources over HTTP using Minimal API style (`app.MapGet`, `app.MapPost`, etc.) |
| Contract | Checked-in `openapi.yaml` is the **source of truth**; ChangeRequests may **patch** the contract when adding/changing operations |
| Change kinds | `feature` or `bugfix` only (single ChangeRequest JSON shape) |
| Auth | **None** — no JWT, OAuth, cookies, or identity frameworks |
| Persistence | **In-memory only** — use `Dictionary`, `List`, or similar; no EF Core, no SQL, no external databases |
| Dependencies | No extra NuGet packages beyond fixture baseline (Minimal API + OpenAPI + xUnit packages already present) |
| File structure | All endpoint code in `MiniErp/Program.cs`; no separate controller classes |
| Tests | Contract tests in `MiniErp.ContractTests/` using `WebApplicationFactory` + xUnit |

## Forbidden (out of scope)

| Category | Examples |
|----------|----------|
| UI/clients | Blazor, WPF, console apps, mobile clients |
| Persistence | EF Core, SQL Server, SQLite, Redis, external databases |
| Auth | JWT, OAuth 2.0, cookies, ASP.NET Identity, IdentityServer |
| Architecture | Multiple projects, microservices, message queues, gRPC |
| External HTTP | Calling third-party APIs as a feature (fixture may not add HttpClient dependencies) |
| Code style | MVC controllers (use Minimal API), separate Services/Repositories folders |

## In-bounds demo scenarios

| Scenario | What changes |
|----------|--------------|
| Add Product resource | `openapi.yaml` (new paths/schemas), `Program.cs` (MapGet/MapPost), contract tests |
| Add Customer resource | Same pattern as Product — new REST resource with in-memory store |
| Fix 404 handling | `Program.cs` (return NotFound instead of throwing), contract tests for edge case |
| Add validation | `Program.cs` (BadRequest for invalid input), `openapi.yaml` (error schema), tests |

## Analyst behavior

1. Evaluate bounds **first** before producing StructuredChange.
2. If **out of bounds**, return only: `{ "inBounds": false, "reason": "<short explanation>" }` — no StructuredChange published.
3. If **in bounds**, return StructuredChange JSON matching `schemas/structured-change.schema.json`.

## Planner / Coder constraints

| Constraint | Detail |
|------------|--------|
| Paths | Relative only — never absolute paths or `..` |
| Coordinated changes | When API surface changes, patch `openapi.yaml` + `MiniErp/Program.cs` + contract tests together |
| Diff format | Unified diffs only; operations: `modify` / `create` / `delete` |
| Target framework | Must be `net9.0` |
| Entry project | `MiniErp/MiniErp.csproj` |

## CodeContext caps (deterministic gate)

| Cap | Value |
|-----|-------|
| Max files loaded | 16 |
| Max total chars | 64,000 |
| Extensions | `.cs`, `.csproj`, `.yaml`, `.yml`, `.json` |

Over-cap fixture → CodeContextBuilder aborts before Planner runs.
