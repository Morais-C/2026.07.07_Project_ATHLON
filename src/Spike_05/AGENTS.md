# Spike_05 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read [ROADMAP.md](../../ROADMAP.md) and [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update **all** spike docs + [ROADMAP.md](../../ROADMAP.md) when a phase completes.
5. **Do not modify `src/Spike_01/` … `src/Spike_04/`.** Those stay frozen archives.

**Current state:** Phase 4 ✅ complete — next is **Phase 5** (thesis + pack tests).

## Scope rules

- **In scope:** Archetype pack infrastructure + formal **`console-v1`** pack; regression parity with Spike_04.
- **Out of scope:** `rest-api-v1` (Spike_06), promotion, portal/API, git, new agent types.
- **No scope creep:** Pack extraction only — do not change chain semantics unless fixing pack wiring bugs.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` inside `src/Spike_05/` |
| Spike_01–04 | Frozen — never edit for Spike_05 features |
| Archetype id | `console-v1` (product); default fixture `echo-v1` (baseline) |
| Pack root | `archetypes/console-v1/` with manifest + 10 components |
| Engine | Same chain as Spike_04: ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier |
| Handoff | Artifact id only; fail fast; no Enter pauses |

## Suggested opening prompt

```text
Read ROADMAP.md and src/Spike_05/AGENTS.md.
Spike_01–04 are frozen. Start Spike_05 Phase 5 per src/Spike_05/ImplementationPlan.md.
Goal: thesis tests + prove agent prompt paths come from pack manifest.
No rest-api-v1. No promotion. One phase at a time; update docs on phase end.
```

## Definition of done

All README success criteria checked; pack loader + `console-v1` pack complete; Spike_04-equivalent tests green through pack paths.
