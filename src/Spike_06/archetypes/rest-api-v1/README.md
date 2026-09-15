# rest-api-v1 — Athlon Solution Archetype pack

> **Status:** **Proved (pack)** — Spike_06 complete (2026-09-15). Sole runtime source for REST prompts/schemas; tests + live demo green via pack loader.  
> Formal pack for the first commercial Athlon Solution Archetype. See [Solution Archetype definition](../../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).

## 10 pack components

| # | Component | Location | Status |
|---|-----------|----------|--------|
| 1 | Identity | `archetype.json` | ✅ Complete |
| 2 | Bounds | `bounds.md` | ✅ REST-accurate |
| 3 | CodeContext | `code-context.md` + manifest caps | ✅ REST-accurate |
| 4 | ChangeRequest profile | `change-request.md` | ✅ REST-accurate |
| 5 | Schemas | `schemas/` | ✅ Complete (8 schemas) |
| 6 | Prompts | `prompts/` | ✅ REST-specific (Analyst, Planner, Coder) |
| 7 | Patch/apply | `patch-apply.md` | ✅ Complete |
| 8 | Proof pipeline | `proof/pipeline.json` | ✅ 4 gates (apply, build, openapi, tests) |
| 9 | Baseline fixture | `../../fixtures/mini-erp-v1` | ✅ Minimal API + OpenAPI + contract tests |
| 10 | Demo ChangeRequests | `demos/change-requests.json` | ✅ 2 features + 1 bugfix |

## Baseline fixture

**`mini-erp-v1`** — near-empty mini-ERP using ASP.NET Minimal API (net9.0):
- `GET /health` endpoint with contract test
- Checked-in `openapi.yaml` (source of truth)
- `WebApplicationFactory` + xUnit for contract tests

## Host configuration

`Athlon:ArchetypeId` = `rest-api-v1` (rest-api-only; no `console-v1` in Spike_06).

## Proof gates

1. **Apply** — unified diffs apply cleanly to copied baseline
2. **Build** — `dotnet build` succeeds
3. **OpenAPI** — published `openapi.yaml` is valid; operations covered by tests
4. **Contract tests** — `dotnet test` passes on contract-test project
