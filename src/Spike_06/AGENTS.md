# Spike_06 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read [ROADMAP.md](../../ROADMAP.md) and [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update **all** spike docs + [ROADMAP.md](../../ROADMAP.md) when a phase completes.
5. **Do not modify `src/Spike_01/` … `src/Spike_05/`.** Those stay frozen archives.

**Current state:** ✅ **Complete (2026-09-15)** — all phases done. **Frozen archive.** Next: **Spike_07** (sequential CR chain), then **Promotion** ([PromotionPlan.md](../../PromotionPlan.md)).

## Scope rules

- **In scope:** `rest-api-v1` pack; `mini-erp-v1` fixture; Applier OpenAPI + contract-test gates; reuse `ArchetypePackLoader`.
- **Out of scope:** `console-v1` in this spike, promotion, portal/API, auth, EF/SQL, client archetypes.
- **No scope creep:** Same chain as Spike_05; extend proof gates only.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` inside `src/Spike_06/` |
| Spike_01–05 | Frozen — never edit for Spike_06 features |
| Archetype id | `rest-api-v1` only (host default) |
| Fixture id | `mini-erp-v1` |
| Pack root | `archetypes/rest-api-v1/` with manifest + 10 components |
| OpenAPI | Checked-in SoT; ChangeRequests may patch contract + code + tests |
| Proof | apply → build → OpenAPI consistency → contract tests |
| Handoff | Artifact id only; fail fast; no Enter pauses |

## Suggested opening prompt (Spike_07)

```text
Spike_06 complete (rest-api-v1 single-hop proved). Next: Spike_07 sequential CR chain.
Read ROADMAP.md and src/Spike_07/AGENTS.md. Spike_01–06 are frozen — do not modify them.
Start Spike_07 Phase 0 per src/Spike_07/ImplementationPlan.md.
No promotion. No portal/API. One phase at a time; update docs on phase end.
```

## Definition of done

All README success criteria checked; live demo apply + all proof gates OK; ROADMAP marks Spike_06 complete.
