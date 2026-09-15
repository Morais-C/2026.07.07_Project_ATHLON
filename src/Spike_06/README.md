# Spike_06 — `rest-api-v1` archetype pack

> **Status:** ✅ Complete (2026-09-15) — **frozen archive**  
> **Depends on:** Spike_05 complete (frozen at [`../Spike_05/`](../Spike_05/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — `rest-api-v1` **before** promotion  
> **Vision:** [REST API competitive positioning](../../Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md) · [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md)  
> **Phase progress:** [ImplementationPlan.md §9 Checklist](./ImplementationPlan.md#9-checklist-tracker)

## Purpose

Spike_05 proved the **archetype pack** model on `console-v1`. Spike_06 answers:

**Can the same loader + change chain govern incremental change on a bounded REST API (`rest-api-v1` / `mini-erp-v1`) with build + OpenAPI + contract-test proof?**

## Thesis (one line)

Load **`archetypes/rest-api-v1/`** → ChangeRequest on near-empty mini-ERP → Analyst/Planner/Coder via pack → Applier → **apply + build + OpenAPI consistency + contract tests**.

## Non-goals

- `console-v1` in this spike (proved in Spike_05)
- Promotion to `Athlon.*` (after Spike_06)
- Portal / API / git / auth / EF/SQL

## Success criteria

- [x] Spike_06 forked from Spike_05; Spike_01–05 untouched
- [x] `archetypes/rest-api-v1/` pack with all 10 components (Phase 1 complete)
- [x] Host defaults to `archetypeId = rest-api-v1` (rest-api-only)
- [x] Fixture `mini-erp-v1`: Minimal API + checked-in OpenAPI + contract tests
- [x] Applier proof: apply + `dotnet build` + OpenAPI consistency + `dotnet test` (Phase 2 complete)
- [x] Demo ChangeRequests: 2 features (add product/customer) + 1 bugfix in pack
- [x] Thesis: LoadAsync-only handoff + pack-resolved paths (Phase 3)
- [x] Live demo: all proof gates OK under `Publish/{workflowId}/` (Phase 4)

## How to run

From `src/Spike_06/` (so `./artifacts`, `./Publish`, `./appsettings*.json`, and `./archetypes/` resolve):

```powershell
dotnet build Spike_06.sln
dotnet test Spike_06.sln   # full suite (84 tests)

# Select active demo via appsettings (Athlon:DemoId) or ATHLON_DEMO_ID env var
# Catalog: archetypes/rest-api-v1/demos/change-requests.json

# Live demo (OpenRouter) — default ChangeRequest: add-product-resource
# Copy appsettings.Local.json.example → appsettings.Local.json and set ApiKey
dotnet run --project Athlon.Spike.Console --no-launch-profile
```

Config: `Athlon:ArchetypeId` = `rest-api-v1`. Optional `OPENROUTER_BASE_URL` overrides the default OpenRouter origin for an OpenAI-compatible endpoint.

## Live demo (2026-09-15)

| Item | Result |
|------|--------|
| Archetype | `rest-api-v1` v1.0.0 via `ArchetypePackLoader` |
| Demo | feature — Add product resource (`add-product-resource`) |
| Chain | ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier |
| Apply | OK |
| Build | OK (`MiniErp/MiniErp.csproj`) |
| OpenAPI | OK — GET `/health`, POST `/products`, GET `/products/{id}` covered |
| Contract tests | OK (3 passed) |
| Publish | `Publish/b3afd403-ddc1-4032-aad6-dd14bb69852c/` with in-memory Product endpoints |
| Tests | `dotnet test` — 84 passed |

## Layout

```text
src/Spike_06/
  Spike_06.sln
  fixtures/mini-erp-v1/          # baseline (component 9)
  archetypes/rest-api-v1/        # formal pack
```

See [archetypes/rest-api-v1/README.md](./archetypes/rest-api-v1/README.md) for component checklist.

## After Spike_06

**Promotion** — lift engine + pack model → `Athlon.*`. Then PoC Sprint 1 (portal/API).
