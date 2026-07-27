# Spike_02 — Agent Instructions

Reference instructions for the **completed** Spike_02 archive. Do not re-implement phases.

## Current state

**Spike_02 is complete** (Phases 0–6). Checklist and README success criteria are met.  
**Next project work:** [Spike_03](../Spike_03/AGENTS.md) (console publish via artifacts), **then** promotion ([PromotionPlan.md](../../PromotionPlan.md)).

## Start here (reference only)

1. Read [ROADMAP.md](../../ROADMAP.md) for sequencing.
2. Read [README.md](./README.md) for thesis, demo, and success criteria.
3. Use [ImplementationPlan.md](./ImplementationPlan.md) as the historical build record (checklist all ✅).
4. **Do not modify this folder** for Spike_03 or promotion feature work — copy forward into Spike_03 instead.
5. **Do not start new Spike_02 phases** — the spike is closed.

## What this folder is

| Role | Detail |
|------|--------|
| Proven reference | BA → StructuredRequirement → Developer → Implementation via artifacts |
| Demo | `dotnet run --project Athlon.Spike.Console` from `src/Spike_02` |
| Thesis proof | `ArtifactHandoffThesisTests` — Developer prompt from `LoadAsync(id)` only |
| Copy source | Baseline for Spike_03 Phase 0 |

## Architecture constraints (still true)

| Rule | Detail |
|------|--------|
| Artifacts | Immutable JSON files — never overwrite |
| LLM access | Agents call `ILLMProvider` only |
| Chain | Developer must not receive BA completion text — only persisted artifact by id |
| BA rigor | Schema validation + **1 required retry** (same as Developer) |
| Host (spike) | Hardcoded sample need; no CLI flag parser |

## Suggested opening prompt (Spike_03 — not Spike_02)

```text
Spike_02 is complete/frozen. Implement Spike_03 per src/Spike_03/ImplementationPlan.md.
Copy from Spike_02 in Phase 0; do not modify src/Spike_02 or src/Spike_01.
```

## Definition of done (Spike_02)

All success criteria in Spike_02 README are checked, thesis test passes, slim console demo runs end-to-end with real LLM. **Met (Phase 6).**
