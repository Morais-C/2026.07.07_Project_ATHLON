# Spike_02 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read project [ROADMAP.md](../../ROADMAP.md) (decision: Spike_02 before promotion; pre-locks L1–L6).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update the **Checklist tracker** in the plan as each phase completes.
5. **Do not modify `src/Spike_01/`.** This folder is a fork/copy; Spike_01 stays the archive demo.

**Current state:** Phase 0 complete (baseline copy builds + 14 tests pass). Continue at **Phase 1 — StructuredRequirement**.

## Scope rules

- **In scope:** Only what appears in ImplementationPlan Phases 0–6 and ROADMAP locks L1–L6.
- **Out of scope:** API, portal, SQL, LangGraph, MCP, memory/RAG, promotion, agents beyond BA + Developer.
- **No scope creep:** If a task is not on the phase list, do not add it without an explicit spike amendment.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` project names inside `src/Spike_02/` |
| Spike_01 | Frozen — never edit for Spike_02 features |
| Artifacts | Immutable JSON files — never overwrite |
| LLM access | Agents call `ILLMProvider` only |
| Chain | Developer must not receive BA completion text — only persisted artifact by id |
| BA rigor | Schema validation + **1 required retry** (same as Developer) |

## Required behaviors (easy to skip — don't)

| Behavior | Where |
|----------|-------|
| StructuredRequirement schema | Plan §7 — not raw `{ text }` echo |
| BA 1 retry on schema failure | Plan Phase 2 |
| Mid-chain approval stub | Plan Phase 4 |
| Thesis test (prompt from LoadAsync only) | Plan Phase 5 / tests |
| JSON fence stripping | Both agents’ validators |

## Conventions

- **Target framework:** `net9.0` (same as Spike_01 baseline)
- **Secrets:** `.env` gitignored — never commit keys
- **Stop after each phase** for human verification when the user asks for baby steps

## Suggested opening prompt

```text
Implement Spike_02 per src/Spike_02/ImplementationPlan.md.

Read ROADMAP.md, src/Spike_02/AGENTS.md and README.md first.
Baseline is a copy of Spike_01 — start at Phase 0 then Phase 1.
Complete one phase at a time and update the checklist.
Do not modify src/Spike_01. No promotion. No portal/API.
```

## Definition of done

All success criteria in Spike_02 README are checked, thesis test passes, and the Phase 6 demo runs end-to-end.
