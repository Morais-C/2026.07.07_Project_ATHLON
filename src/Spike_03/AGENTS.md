# Spike_03 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read project [ROADMAP.md](../../ROADMAP.md) (Spike_03 **before** promotion; locks in ImplementationPlan §2).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update **only** the [Checklist tracker](./ImplementationPlan.md#9-checklist-tracker) when a phase completes.
5. **Do not modify `src/Spike_01/` or `src/Spike_02/`.** Spike_03 is a fork/copy; those stay archives.

**Current state:** Phase 4 complete — next is **Phase 5 (thesis + E2E CI tests)**.

## Scope rules

- **In scope:** ImplementationPlan Phases 0–6 and locks L1–L12.
- **Out of scope:** Promotion, API, portal, SQL, LangGraph, MCP, RAG, CLI flags, apps outside console bounds.
- **No scope creep:** If it is not on the phase list, do not add it without an explicit spike amendment.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` project names inside `src/Spike_03/` |
| Spike_01 / Spike_02 | Frozen — never edit for Spike_03 features |
| Roster | Analyst → Planner → Coder (LLM `IAgent`); **Publisher** deterministic (not LLM) |
| Artifacts | Immutable JSON in `artifacts/`; Publish tree immutable under `Publish/{workflowId}/` |
| Handoff | Next step gets **artifact id only** — never prior raw LLM completion |
| Bounds | Analyst fail ASAP if not read→process→print net9 console (≤3 source files) |
| LLM access | Agents call `ILLMProvider` only |
| Host | Hardcoded need; appsettings; Enter pause after Analyst/Planner/Coder |
| Publisher | Deterministic materialize + `dotnet build` only (L10); no functional run |

## Required behaviors (easy to skip — don't)

| Behavior | Where |
|----------|-------|
| Analyst abort without SR publish | Phase 1 / L7 |
| Schema + 1 retry on Planner & Coder | Phases 1–2 |
| CodePackage path safety | Phase 2–3 |
| Publisher build (materialize + `dotnet build`) | Phase 3 / 5 |
| Thesis: LoadAsync-only prompts | Phase 5 |
| Remove Spike_02 dead ends (legacy workflow/skips) | Phase 4 |
| No CLI creep | Entire spike |

## Conventions

- **Target framework:** `net9.0`
- **Secrets:** `appsettings.Local.json` gitignored — never commit keys
- **Stop after each phase** when the user asks for baby steps
- **Progress:** checklist in ImplementationPlan only

## Suggested opening prompt

```text
Finish Spike_03 per src/Spike_03/ImplementationPlan.md.

Read ROADMAP.md, src/Spike_03/AGENTS.md and README.md first.
Phases 0–4 are done — start at Phase 5 (thesis + E2E tests). Then Phase 6 (demo).
One phase at a time; update only the checklist.
Publisher is build-only (L10). Do not modify src/Spike_01 or src/Spike_02. No promotion. No portal/API.
```

## Definition of done

All success criteria in Spike_03 README are checked; thesis + Publisher **build** E2E tests pass;
demo publishes a console under `Publish/{workflowId}/` (functional run checks deferred to Tester).
