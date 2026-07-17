# Spike_01 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read [README.md](./README.md) for purpose, scope, and success criteria.
2. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time** (Phase 0 → 6).
3. Update the **Checklist tracker** (§11 in the plan) as each phase completes.

**Current state:** Spike_01 complete. Project decision (2026-07-17): next is **Spike_02** (BA → Developer via artifacts), **before** promotion. See [ROADMAP.md](../../ROADMAP.md).

## Scope rules

- **In scope:** Only what appears in ImplementationPlan §2 and Phases 0–6.
- **Out of scope:** API, portal, SQL, LangGraph, MCP, memory/RAG, multiple agents, Docker/CI — see README and plan §2.
- **No scope creep:** If a task is not on the phase list, do not add it without an explicit spike amendment.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Projects use `Athlon.Spike.*` prefix until promotion (plan §10) |
| Dependencies | `Contracts` has no refs; `Console` is composition root only (plan §3–4) |
| Artifacts | Immutable JSON files — never overwrite (plan Phase 2) |
| LLM access | Agents call `ILLMProvider` only — no direct OpenRouter HTTP in agent code |
| Workflow | Nothing runs outside a workflow instance (plan Phase 5) |

## Required behaviors (easy to skip — don't)

| Behavior | Where |
|----------|-------|
| JSON fence stripping | Plan §8 risk table; validator handles ` ```json ` wrappers |
| **1 retry on schema failure** | Plan Phase 4 — **required**, not optional |
| Retry test | `MockLLMProvider` returns invalid JSON once, valid on second call — agent must publish |
| Run telemetry | Print tokens, duration, est. cost per workflow (README) |

## VisionScope references

Use the manuscript for **principles only** — do not re-read the full book.

| Topic | Reference |
|-------|-----------|
| Agent lifecycle | Ch. 6 |
| Workflow orchestration | Ch. 7 |
| Artifacts | Ch. 8 |
| Build order | Appendix A §A.15 |
| `IArtifactStore` | Appendix B §B.7 |

Full index: [Project_ATHLON_VisionScope/INDEX.md](../../Project_ATHLON_VisionScope/INDEX.md)

## Conventions

- **Target framework:** `net10.0` (fall back to `net9.0` if SDK unavailable — document in README)
- **Secrets:** `OPENROUTER_API_KEY`, `OPENROUTER_MODEL` via environment — never commit
- **Gitignore:** `artifacts/`, `bin/`, `obj/`, `.env`
- **Tests:** Add `Athlon.Spike.Tests` in Phase 2 if time allows; mock LLM for CI, OpenRouter smoke manual only

## Suggested opening prompt

```text
Implement Spike_01 per src/Spike_01/ImplementationPlan.md.

Read src/Spike_01/AGENTS.md and README.md first.
Start at Phase 0. Complete one phase at a time and update the checklist.
Phase 4 retry-on-invalid-JSON is required. No scope beyond the plan.
```

## Definition of done

All items in README **Success criteria** are checked, and the Phase 6 demo script in the plan runs end-to-end.
