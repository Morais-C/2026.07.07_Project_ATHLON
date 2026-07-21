# Spike_02 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read project [ROADMAP.md](../../ROADMAP.md) (Spike_02 before promotion; locks L1–L6 — **L4 amended** for minimal host).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update **only** the [Checklist tracker](./ImplementationPlan.md#12-checklist-tracker) when a phase completes (that table is the single source of truth).
5. **Do not modify `src/Spike_01/`.** This folder is a fork/copy; Spike_01 stays the archive demo.

## Scope rules

- **In scope:** Only what appears in ImplementationPlan Phases 0–6 (including **0.5**) and ROADMAP locks (with host amendment).
- **Out of scope:** API, portal, SQL, LangGraph, MCP, memory/RAG, promotion, agents beyond BA + Developer, **CLI flags**.
- **No scope creep:** If a task is not on the phase list, do not add it without an explicit spike amendment.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` project names inside `src/Spike_02/` |
| Spike_01 | Frozen — never edit for Spike_02 features |
| Artifacts | Immutable JSON files — never overwrite |
| LLM access | Agents call `ILLMProvider` only; demos use real OpenRouter via `appsettings.json` |
| Host | Hardcoded sample need; no `--text` / `--input` / `--load` / `--auto-approve` |
| Chain | Developer must not receive BA completion text — only persisted artifact by id |
| BA rigor | Schema validation + **1 required retry** (same as Developer) |
| Comments | Short training comments on handoff/schema steps — not noise |

## Required behaviors (easy to skip — don't)

| Behavior | Where |
|----------|-------|
| StructuredRequirement schema | Plan §8 — not raw `{ text }` echo |
| BA 1 retry on schema failure | Plan Phase 2 |
| Optional Enter pause after BA | Plan Phase 0.5 / 4 |
| Thesis test (prompt from LoadAsync only) | Plan Phase 5 / tests |
| JSON fence stripping | Both agents’ validators |
| Slim Program.cs | Plan Phase 0.5 — do not reintroduce CLI |

## Conventions

- **Target framework:** `net9.0` (same as Spike_01 baseline)
- **Secrets:** `appsettings.Local.json` gitignored — never commit keys
- **Stop after each phase** for human verification when the user asks for baby steps
- **Progress:** checklist in ImplementationPlan only — do not mirror phase status elsewhere

## Suggested opening prompt

```text
Implement Spike_02 per src/Spike_02/ImplementationPlan.md.

Read ROADMAP.md, src/Spike_02/AGENTS.md and README.md first.
Check the Checklist tracker in ImplementationPlan.md §12 for the next phase.
Complete one phase at a time; update only that checklist; stop for human verification.
Do not modify src/Spike_01. No promotion. No portal/API. No CLI flags.
```

## Definition of done

All success criteria in Spike_02 README are checked, thesis test passes, slim console demo runs end-to-end with real LLM.
