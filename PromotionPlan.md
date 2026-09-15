# PromotionPlan — Spike → `Athlon.*`

> **Status:** **Blocked** — complete **Spike_07** (sequential ChangeRequests / evolving Publish baseline) first.  
> **Source of truth for sequencing:** [ROADMAP.md](./ROADMAP.md)  
> **Promotion input (target):** [`src/Spike_07/`](./src/Spike_07/) when proven (Spike_01–06 remain frozen reference; do not start `Athlon.*` yet)

## Goal

Lift the proven spike agent/artifact stack **and archetype pack model** into product namespaces so PoC Sprint 1 (API + portal) can depend on `Athlon.*` instead of `Athlon.Spike.*`.

```text
Athlon.Spike.Contracts / Artifacts / Llm / Agents / Workflow / ArchetypePacks
        →
Athlon.Contracts / Artifacts / Llm / Agents / Workflow / ArchetypePacks
(+ keep or slim a host; API comes in Sprint 1)
```

## Why promotion waits

| Milestone | What it proves |
|-----------|----------------|
| Spike_04 ✅ | Incremental console change via artifacts (engine) |
| Spike_05 ✅ | Formal **`console-v1`** pack + `ArchetypePackLoader` (product cornerstone) |
| Spike_06 ✅ | Second pack **`rest-api-v1`** reuses loader (commercial SKU path) |
| Spike_07 | Sequential CR chain — hop 2 baseline = prior **Publish** tree (evolving solution) |
| **Promotion** | Lift engine + packs — not spike-root hardcoded prompts |

Promoting after Spike_06 alone would lift **single-hop** proof without chained Publish baselines. Spike_07 establishes sequential evolution before product namespaces.

## Non-goals (this milestone)

- Reopening Spike_01–06 spike phases (archives — reference only)
- Portal / API / SQL / LangGraph / RAG / MCP (Sprint 1+)
- Editing frozen spike folders for product features (copy from latest proven spike into `Athlon.*` instead)

## Suggested phases (draft — refine at P0)

| Phase | Intent | Exit sketch |
|-------|--------|-------------|
| P0 | Inventory Spike_07 projects, tests, schemas, **packs**, prompts; map → `Athlon.*` | Written mapping table |
| P1 | Create `Athlon.*` projects / solution; move or copy code; rename namespaces | Solution builds |
| P2 | Port tests; thesis + publish/apply E2E + **pack loader** still green | `dotnet test` green |
| P3 | Wire a minimal host or leave spike console as smoke until API exists | Documented run path |
| P4 | Update ROADMAP / VisionScope pointers; freeze spikes as reference | Checklist done |

## Constraints to preserve

| Rule | Why |
|------|-----|
| Immutable artifacts (+ immutable Publish trees) | Core thesis |
| `ILLMProvider` only inside LLM agents | No HTTP leakage into agent layer |
| Next step loads prior output **by artifact id only** | Thesis tests must still hold |
| Schema + 1 retry on LLM agents | Same rigor as spikes |
| Deterministic Publisher/Applier for disk/build | Don’t pretend build/apply is an LLM job |
| **Archetype packs** resolved by id (not hardcoded spike paths) | Product cornerstone |
| Secrets via `appsettings.Local.json` — never commit keys | Safety |

## Fresh session starter

```text
Promotion is blocked until Spike_07. If starting Spike_07, read src/Spike_07/AGENTS.md instead.
When Spike_07 is complete: read ROADMAP.md and this PromotionPlan.md.
Spike_01–07 frozen — copy proven code into Athlon.*; do not edit spikes for product features.
No portal/API until after promotion. Do not start Athlon.* until promotion begins.
```

## Checklist tracker

| Phase | Status |
|-------|--------|
| P0 — Inventory & mapping | ⬜ Blocked (Spike_07 first) |
| P1 — Athlon.* projects + rename | ⬜ Not started |
| P2 — Tests / thesis / packs green | ⬜ Not started |
| P3 — Host / smoke path | ⬜ Not started |
| P4 — Docs freeze spikes | ⬜ Not started |
