# rest-api-v1 — Bounds checklist

Analyst + Planner + Coder rejection rules. Violation → **fail fast** without publishing downstream artifacts.

## In scope (ALL must be true)

| Rule | Detail |
|------|--------|
| Target runtime | Single existing **.NET 9 ASP.NET Minimal API** (checked-in `mini-erp-v1` baseline) |
| App pattern | REST resources over HTTP; OpenAPI-first; in-memory store when persistence is needed |
| Contract | Checked-in OpenAPI (`openapi.yaml` / `.json`) is source of truth; ChangeRequests may **patch** the contract when adding/changing operations |
| Change kinds | `feature` or `bugfix` only (single ChangeRequest JSON shape) |
| Auth | **None** in Spike_06 |
| Persistence | **In-memory only** — no EF, SQL, or external DB |
| Dependencies | No extra NuGet beyond fixture baseline (Minimal API + OpenAPI + contract-test packages already present) |
| Forbidden | Console/GUI clients, auth stacks, EF/SQL, arbitrary external HTTP as a feature, multi-solution sprawl beyond fixture |

## Mental demos that fit

Add product / customer resources with CRUD-ish endpoints; fix malformed-id status codes; align OpenAPI + code + contract tests in one PatchPackage.

## Analyst behavior

1. Evaluate bounds **first** before producing StructuredChange.
2. If **out of bounds**, return only: `{ "inBounds": false, "reason": "<short explanation>" }` — no StructuredChange published.
3. If **in bounds**, return StructuredChange JSON matching `schemas/structured-change.schema.json`.

## Planner / Coder bounds (same scope)

- Relative paths only — never absolute paths or `..`
- Prefer patching **OpenAPI + API project + contract tests** together when the surface changes
- PatchPackage: unified diffs only; operations `modify` \| `create` \| `delete`; `targetFramework` must be `net9.0`

## CodeContext caps (deterministic gate)

| Cap | Value |
|-----|-------|
| Max files loaded | 16 |
| Max total chars | 64,000 |
| Extensions | `.cs`, `.csproj`, `.yaml`, `.yml`, `.json` |

Over-cap fixture → CodeContextBuilder aborts before Planner runs.

## Out-of-scope examples

Blazor/WPF, SQL persistence, JWT/OAuth, generating clients, calling third-party SaaS as a feature, rewriting into a multi-service mesh.
