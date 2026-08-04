# console-v1 — Athlon Solution Archetype pack

> **Status:** **Proved (pack)** — Spike_05 complete (2026-08-04). Sole runtime source for prompts/schemas; tests + live demo green via pack loader.

Formal pack for the `console-v1` archetype. See [Solution Archetype definition](../../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).

## Layout (10 components)

| # | Component | Path | Phase 1 |
|---|-----------|------|---------|
| 1 | Identity | `archetype.json` | ✅ Manifest + artifact chain + agent roster |
| 2 | Bounds | `bounds.md` | ✅ Analyst/Planner/Coder rules |
| 3 | CodeContext rules | `code-context.md` + manifest caps | ✅ Caps + loading rules |
| 4 | ChangeRequest profile | `change-request.md` | ✅ Feature/bugfix fields |
| 5 | Schemas | `schemas/` | ✅ 8 JSON schemas (chain + legacy) |
| 6 | Prompts | `prompts/` | ✅ analyst, planner, coder |
| 7 | Patch/apply rules | `patch-apply.md` | ✅ Unified diff + Applier conventions |
| 8 | Proof pipeline | `proof/pipeline.json` | ✅ Apply + build gates |
| 9 | Baseline fixture | `../../fixtures/echo-v1` | ✅ Spike root (unchanged) |
| 10 | Demo ChangeRequests | `demos/change-requests.json` | ✅ Uppercase feature + trim bugfix |

## Artifact chain

```text
ChangeRequest → Analyst → StructuredChange
                        → CodeContextBuilder → CodeContext
                        → ChangeBundle → Planner → ImplementationPlan
                        → Coder → PatchPackage → Applier → Publish/{workflowId}/
```

## Baseline

Default fixture: **`echo-v1`** at `src/Spike_05/fixtures/echo-v1/` (referenced from manifest `baseline.fixturePath`).

## Runtime note

Host + tests resolve prompts/schemas **only** from this pack via `ArchetypePackLoader`. Spike-root `prompts/` and `schemas/` duplicates were removed in Phase 4.

## Demo ChangeRequests

| Id | Kind | Title |
|----|------|-------|
| `uppercase-echo` | feature | Uppercase echo |
| `trim-echo` | bugfix | Trim echo input |

Host may still hardcode the active demo in `Program.cs`; catalog lives here for Spike_05/06 handoff.
