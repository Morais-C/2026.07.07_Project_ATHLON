# PromotionPlan — Spike → `Athlon.*`

> **Status:** Stub — **deferred until Spike_03 succeeds**.  
> **Source of truth for sequencing:** [ROADMAP.md](./ROADMAP.md)  
> **Current next milestone:** [Spike_03](./src/Spike_03/AGENTS.md)  
> **Likely promotion input:** [`src/Spike_03/`](./src/Spike_03/) once complete (else Spike_02)

## Goal

Lift the proven spike agent/artifact stack into product namespaces so PoC Sprint 1 (API + portal) can depend on `Athlon.*` instead of `Athlon.Spike.*`.

```text
Athlon.Spike.Contracts / Artifacts / Llm / Agents / Workflow
        →
Athlon.Contracts / Artifacts / Llm / Agents / Workflow
(+ keep or slim a host; API comes in Sprint 1)
```

## Non-goals (this milestone)

- Starting before Spike_03 is done (see ROADMAP 2026-07-27)
- Portal / API / SQL / LangGraph / RAG / MCP (Sprint 1+)
- Editing `src/Spike_01/`

## Suggested phases (draft — refine after Spike_03)

| Phase | Intent | Exit sketch |
|-------|--------|-------------|
| P0 | Inventory Spike_03 (or Spike_02) projects, tests, schemas, prompts; map → `Athlon.*` | Written mapping table |
| P1 | Create `Athlon.*` projects / solution; move or copy code; rename namespaces | Solution builds |
| P2 | Port tests; thesis + publish E2E still green under new names | `dotnet test` green |
| P3 | Wire a minimal host or leave spike console as smoke until API exists | Documented run path |
| P4 | Update ROADMAP / VisionScope pointers; freeze spikes as reference | Checklist done |

## Constraints to preserve

| Rule | Why |
|------|-----|
| Immutable artifacts (+ immutable Publish trees) | Core thesis |
| `ILLMProvider` only inside LLM agents | No HTTP leakage into agent layer |
| Next step loads prior output **by artifact id only** | Thesis tests must still hold |
| Schema + 1 retry on LLM agents | Same rigor as spikes |
| Deterministic Publisher for disk/build | Don’t pretend build is an LLM job |
| Secrets via `appsettings.Local.json` — never commit keys | Safety |

## Fresh session starter (only after Spike_03 ✅)

```text
Read ROADMAP.md and PromotionPlan.md.
Spikes are complete — do not reopen spike phases.
Promote Athlon.Spike.* into Athlon.* (prefer Spike_03 as input).
Keep spike folders intact as reference until promotion tests pass.
Do not modify src/Spike_01. No portal/API until promotion is done.
```

## Checklist tracker

| Phase | Status |
|-------|--------|
| P0 — Inventory & mapping | ⬜ Blocked on Spike_03 |
| P1 — Athlon.* projects + rename | ⬜ Not started |
| P2 — Tests / thesis green | ⬜ Not started |
| P3 — Host / smoke path | ⬜ Not started |
| P4 — Docs freeze spikes | ⬜ Not started |
