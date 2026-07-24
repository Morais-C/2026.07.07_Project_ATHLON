# PromotionPlan — Spike_02 → `Athlon.*`

> **Status:** Stub for the next milestone (after Spike_02 complete).  
> **Source of truth for sequencing:** [ROADMAP.md](./ROADMAP.md)  
> **Proven input:** [`src/Spike_02/`](./src/Spike_02/) (do not modify Spike_01)

## Goal

Lift the proven Spike_02 agent/artifact stack into product namespaces so PoC Sprint 1 (API + portal) can depend on `Athlon.*` instead of `Athlon.Spike.*`.

```text
Athlon.Spike.Contracts / Artifacts / Llm / Agents / Workflow
        →
Athlon.Contracts / Artifacts / Llm / Agents / Workflow
(+ keep or slim a host; API comes in Sprint 1)
```

## Non-goals (this milestone)

- Portal / API / SQL / LangGraph / RAG / MCP (Sprint 1+)
- Rewriting Spike_02 thesis behavior
- Editing `src/Spike_01/`

## Suggested phases (draft — refine before coding)

| Phase | Intent | Exit sketch |
|-------|--------|-------------|
| P0 | Inventory Spike_02 projects, tests, schemas, prompts; map → `Athlon.*` names | Written mapping table |
| P1 | Create `Athlon.*` projects / solution; move or copy code; rename namespaces | Solution builds |
| P2 | Port tests; thesis test still green under new names | `dotnet test` green |
| P3 | Wire a minimal host or leave Spike_02 console as smoke until API exists | Documented run path |
| P4 | Update ROADMAP / VisionScope pointers; freeze Spike_02 as reference | Checklist done |

## Constraints to preserve

| Rule | Why |
|------|-----|
| Immutable artifacts | Core thesis |
| `ILLMProvider` only inside agents | No HTTP leakage into agent layer |
| Developer loads BA output by artifact id only | Thesis test must still hold |
| Schema + 1 retry on BA and Developer | Same rigor as Spike_02 |
| Secrets via `appsettings.Local.json` (or equivalent) — never commit keys | Safety |

## Fresh session starter

```text
Read ROADMAP.md and PromotionPlan.md.
Spike_01 and Spike_02 are complete — do not reopen spike phases.
Draft/execute promotion of src/Spike_02 Athlon.Spike.* into Athlon.*.
Keep src/Spike_02 as the reference until promotion tests pass.
Do not modify src/Spike_01. No portal/API until promotion is done.
```

## Checklist tracker

| Phase | Status |
|-------|--------|
| P0 — Inventory & mapping | ⬜ Not started ← **next** |
| P1 — Athlon.* projects + rename | ⬜ Not started |
| P2 — Tests / thesis green | ⬜ Not started |
| P3 — Host / smoke path | ⬜ Not started |
| P4 — Docs freeze Spike_02 | ⬜ Not started |
