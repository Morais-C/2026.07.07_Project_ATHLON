# Spike_04 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read project [ROADMAP.md](../../ROADMAP.md) (Spike_04 **before** promotion; locks in ImplementationPlan §2).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update **only** the [Checklist tracker](./ImplementationPlan.md#9-checklist-tracker) when a phase completes.
5. **Do not modify `src/Spike_01/`, `src/Spike_02/`, or `src/Spike_03/`.** Those stay archives.

**Current state:** Phase 1 complete — next is **Phase 2 (Planner + Coder → PatchPackage)**.

## Scope rules

- **In scope:** ImplementationPlan Phases 0–6 and locks L1–L14.
- **Out of scope:** Promotion, API, portal, SQL, LangGraph, MCP, RAG, git, CLI flags, Enter pauses, Tester.
- **No scope creep:** If it is not on the phase list, do not add it without an explicit spike amendment.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` project names inside `src/Spike_04/` |
| Spike_01–03 | Frozen — never edit for Spike_04 features |
| Roster | Analyst → Planner → Coder (LLM); **Applier** deterministic (not LLM) |
| Baseline | Checked-in **fixture** under `fixtures/` (not a live Spike_03 Publish run) |
| Context | **CodeContext** = all fixture files (Spike_04); hard caps; abort if over |
| Changes | **PatchPackage** with **unified diffs** only |
| Publish | New `Publish/{workflowId}/` each run (branch metaphor); never overwrite fixture |
| Handoff | Next step gets **artifact id only** |
| Host | Hardcoded ChangeRequest; appsettings; **fail fast** (no Enter pauses) |
| Success | Materialize + apply + `dotnet build` only (L10-style); functional run → Tester later |

## Required behaviors (easy to skip — don't)

| Behavior | Where |
|----------|-------|
| Analyst abort without StructuredChange publish | Phase 1 |
| Schema + 1 retry on Planner & Coder | Phases 1–2 |
| Patch path safety + apply failure = fail fast | Phase 3 |
| Thesis: LoadAsync-only prompts | Phase 5 |
| No Enter pauses; fail fast | Phase 4 / L11 |
| No CLI creep | Entire spike |

## Conventions

- **Target framework:** `net9.0`
- **Secrets:** `appsettings.Local.json` gitignored — never commit keys
- **Stop after each phase** when the user asks for baby steps
- **Progress:** checklist in ImplementationPlan only

## Suggested opening prompt

```text
Finish Spike_04 per src/Spike_04/ImplementationPlan.md.

Read ROADMAP.md, src/Spike_04/AGENTS.md and README.md first.
Phases 0–1 are done — start at Phase 2 (Planner + Coder → PatchPackage).
One phase at a time; update only the checklist.
Do not modify Spike_01–03. No promotion. No portal/API. No Enter pauses.
```

## Definition of done

All success criteria in Spike_04 README are checked; thesis + Applier **build** E2E tests pass;
demo applies a change request onto the fixture and publishes a building console under `Publish/{workflowId}/`.
