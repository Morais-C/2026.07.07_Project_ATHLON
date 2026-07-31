# PromotionPlan — Spike → `Athlon.*`

> **Status:** Ready — **Spike_04 complete** (2026-07-31); promotion is **next**.  
> **Source of truth for sequencing:** [ROADMAP.md](./ROADMAP.md)  
> **Promotion input:** [`src/Spike_04/`](./src/Spike_04/) (proven; Spike_01–03 remain frozen reference)

## Goal

Lift the proven spike agent/artifact stack into product namespaces so PoC Sprint 1 (API + portal) can depend on `Athlon.*` instead of `Athlon.Spike.*`.

```text
Athlon.Spike.Contracts / Artifacts / Llm / Agents / Workflow
        →
Athlon.Contracts / Artifacts / Llm / Agents / Workflow
(+ keep or slim a host; API comes in Sprint 1)
```

## Non-goals (this milestone)

- Reopening Spike_01–04 spike phases (archives — reference only)
- Portal / API / SQL / LangGraph / RAG / MCP (Sprint 1+)
- Editing `src/Spike_01/` / `src/Spike_02/` / `src/Spike_03/` / `src/Spike_04/` for product features (copy from Spike_04 into `Athlon.*` instead)

## Suggested phases (draft — refine at P0)

| Phase | Intent | Exit sketch |
|-------|--------|-------------|
| P0 | Inventory Spike_04 projects, tests, schemas, prompts; map → `Athlon.*` | Written mapping table |
| P1 | Create `Athlon.*` projects / solution; move or copy code; rename namespaces | Solution builds |
| P2 | Port tests; thesis + publish/apply E2E still green under new names | `dotnet test` green |
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
| Secrets via `appsettings.Local.json` — never commit keys | Safety |

## Fresh session starter

```text
Spike_04 is complete (Phases 0–6). Read ROADMAP.md and PromotionPlan.md.
Spike_01–04 are frozen archives — do not modify them for promotion features.
Start promotion of proven Spike_04 code → Athlon.* per PromotionPlan.md.
No portal/API until after promotion.
```

## Checklist tracker

| Phase | Status |
|-------|--------|
| P0 — Inventory & mapping | ⬜ Not started ← **next** |
| P1 — Athlon.* projects + rename | ⬜ Not started |
| P2 — Tests / thesis green | ⬜ Not started |
| P3 — Host / smoke path | ⬜ Not started |
| P4 — Docs freeze spikes | ⬜ Not started |
