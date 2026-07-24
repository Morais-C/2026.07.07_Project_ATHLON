# Spike_02 — Agent Instructions

Reference instructions for the **completed** Spike_02 archive. Do not re-implement phases.

## Current state

**Spike_02 is complete** (Phases 0–6). Checklist and README success criteria are met.  
**Next project work:** promote `Athlon.Spike.*` → `Athlon.*` per [ROADMAP.md](../../ROADMAP.md) and [PromotionPlan.md](../../PromotionPlan.md).

## Start here (reference only)

1. Read [ROADMAP.md](../../ROADMAP.md) for sequencing.
2. Read [README.md](./README.md) for thesis, demo, and success criteria.
3. Use [ImplementationPlan.md](./ImplementationPlan.md) as the historical build record (checklist all ✅).
4. **Do not modify `src/Spike_01/`.** Keep Spike_02 as the proven reference until promotion lands.
5. **Do not start new Spike_02 phases** — the spike is closed.

## What this folder is

| Role | Detail |
|------|--------|
| Proven reference | BA → StructuredRequirement → Developer → Implementation via artifacts |
| Demo | `dotnet run --project Athlon.Spike.Console` from `src/Spike_02` |
| Thesis proof | `ArtifactHandoffThesisTests` — Developer prompt from `LoadAsync(id)` only |

## Architecture constraints (still true for promotion)

| Rule | Detail |
|------|--------|
| Artifacts | Immutable JSON files — never overwrite |
| LLM access | Agents call `ILLMProvider` only |
| Chain | Developer must not receive BA completion text — only persisted artifact by id |
| BA rigor | Schema validation + **1 required retry** (same as Developer) |
| Host (spike) | Hardcoded sample need; no CLI flag parser |

## Conventions

- **Target framework:** `net9.0`
- **Secrets:** `appsettings.Local.json` gitignored — never commit keys
- **Progress for Spike_02:** closed — do not reopen the phase checklist except for errata

## Suggested opening prompt (promotion — not Spike_02)

```text
Spike_02 is complete. Follow ROADMAP.md and PromotionPlan.md.
Promote Athlon.Spike.* from src/Spike_02 into Athlon.* namespaces/projects.
Do not modify src/Spike_01. Keep Spike_02 intact as the reference until promotion is verified.
```

## Definition of done (Spike_02)

All success criteria in Spike_02 README are checked, thesis test passes, slim console demo runs end-to-end with real LLM. **Met (Phase 6).**
