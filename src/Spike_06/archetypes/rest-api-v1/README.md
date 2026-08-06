# rest-api-v1 — Athlon Solution Archetype pack

> **Status:** Training skeleton — Spike_06 Phase 0.  
> Formal pack for the first commercial Athlon Solution Archetype. See [Solution Archetype definition](../../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).

## 10 pack components

| # | Component | Location | Phase 0 |
|---|-----------|----------|---------|
| 1 | Identity | `archetype.json` | ✅ |
| 2 | Bounds | `bounds.md` | ✅ draft |
| 3 | CodeContext | `code-context.md` + manifest caps | ✅ draft |
| 4 | ChangeRequest profile | `change-request.md` | ✅ draft |
| 5 | Schemas | `schemas/` | ✅ carried from Spike_05 |
| 6 | Prompts | `prompts/` | ✅ draft (polish Phase 1) |
| 7 | Patch/apply | `patch-apply.md` | ✅ draft |
| 8 | Proof pipeline | `proof/pipeline.json` | ✅ (4 gates documented) |
| 9 | Baseline fixture | `../../fixtures/mini-erp-v1` | ✅ |
| 10 | Demo ChangeRequests | `demos/change-requests.json` | ✅ product + customer drafts |

## Baseline

Default fixture: **`mini-erp-v1`** — near-empty mini-ERP Minimal API + checked-in OpenAPI + contract tests.

## Host

`Athlon:ArchetypeId` = `rest-api-v1` (rest-api-only; no `console-v1` in Spike_06).
