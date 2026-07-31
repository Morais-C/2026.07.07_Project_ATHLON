# console-v1 — Athlon Solution Archetype pack

> **Status:** Phase 0 skeleton (Spike_05). Content migrates from spike root in Phase 1; loader wires paths in Phases 2–3.

Formal pack for the `console-v1` archetype. See [Solution Archetype definition](../../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).

## Layout (10 components)

| # | Component | Path | Phase 0 |
|---|-----------|------|---------|
| 1 | Identity | `archetype.json` | Draft manifest |
| 2 | Bounds | `bounds.md` | Placeholder |
| 3 | CodeContext rules | `code-context.md` + manifest caps | Placeholder |
| 4 | ChangeRequest profile | `change-request.md` | Placeholder |
| 5 | Schemas | `schemas/` | Empty (Phase 1) |
| 6 | Prompts | `prompts/` | Empty (Phase 1) |
| 7 | Patch/apply rules | `patch-apply.md` | Placeholder |
| 8 | Proof pipeline | `proof/pipeline.json` | Placeholder |
| 9 | Baseline fixture | `../../fixtures/echo-v1` | Spike root (unchanged) |
| 10 | Demo ChangeRequests | `demos/change-requests.json` | Placeholder |

## Baseline

Default fixture: **`echo-v1`** at `src/Spike_05/fixtures/echo-v1/` (Spike_04 layout preserved until Phase 1+ migration decision).

## Runtime (target)

Host selects `archetypeId: console-v1` → `ArchetypePackLoader` resolves all paths from this folder. Until Phase 3, spike-root `prompts/` and `schemas/` remain the runtime source (Spike_04 parity).
