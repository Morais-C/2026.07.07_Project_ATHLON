# Spike_03 — Console Publish via Artifacts

> **Depends on:** Spike_02 complete (frozen at [`../Spike_02/`](../Spike_02/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — Spike_03 **before** promotion  
> **Host:** Minimal console + real OpenRouter LLM (no CLI flags)  
> **Phase progress:** [ImplementationPlan.md §9 Checklist](./ImplementationPlan.md#9-checklist-tracker)

## Purpose

Spike_03 answers:

**Can agents chain through artifacts all the way to buildable, runnable code on disk?**

```text
BusinessRequirement (hardcoded raw need)
        ↓
  AnalystAgent  →  StructuredRequirement
        ↓  (or abort ASAP if not a bounded console)
  PlannerAgent  →  ImplementationPlan
        ↓
  CoderAgent    →  CodePackage
        ↓
  Publisher (deterministic)
        ↓
  Publish/{workflowId}/  + dotnet build/run proof
```

Spike_02 proved agent → artifact → agent.  
Spike_03 proves the chain can **terminate in a published console app**.

## Bounds (fail ASAP)

In scope only if the need is a **single .NET 9 console** that **reads (0+ inputs) → processes → prints**, with ≤3 source files + `.csproj`.  
No web/GUI/DB/network-as-feature. Mental demos: Hello World, echo, calculator, converter.  
Out of bounds → Analyst fails **without** publishing StructuredRequirement.

## Pre-locked decisions

See [ImplementationPlan.md §2](./ImplementationPlan.md#2-pre-locked-decisions-2026-07-27) (L1–L12). Do not reopen mid-spike.

## What this spike does *not* include

Promotion, portal, API, SQL, LangGraph, RAG, MCP, editing Spike_01/Spike_02, CLI flags.

## Prerequisites / config

Same pattern as Spike_02: `appsettings.json` + gitignored `appsettings.Local.json`.

```bash
cd src/Spike_03
cp appsettings.Local.json.example appsettings.Local.json   # then paste your OpenRouter key
```

`OpenRouter:ApiKey` and `OpenRouter:Model` are both required; the host exits with a hint if either is missing.

## Demo (~5–10 min)

Run everything from `src/Spike_03` so `prompts/`, `schemas/`, `artifacts/`, `Publish/` and `appsettings*.json` resolve.

```bash
cd src/Spike_03
dotnet test                                  # 43 tests, no network / no API key needed
dotnet run --project Athlon.Spike.Console    # real OpenRouter run
```

The console prints the workflow id, then pauses for Enter after each LLM agent. At each pause, open the
artifact just written under `artifacts/{workflowId}/` before continuing:

| Pause | Artifact to open | What it shows |
|-------|------------------|---------------|
| After Analyst | `StructuredRequirement` | Raw need turned into structured fields; in bounds |
| After Planner | `ImplementationPlan` | Tasks and acceptance criteria — no source code yet |
| After Coder | `CodePackage` | `files[]`, `entryProject`, `targetFramework` |

After the final Enter the deterministic Publisher materializes `Publish/{workflowId}/` and runs
`dotnet build`. Open that folder and `publish-manifest.json` to see `buildSucceeded` and
`functionalTest: deferred-to-tester-agent` (L10 — functional run belongs to a future Tester agent).

**Optional abort run:** replace the hardcoded `need` in `Program.cs` with something out of bounds
(e.g. a web portal with a SQL database). The Analyst refuses, prints its reason, pauses so you can read
it, and exits with code 2 — no StructuredRequirement, no CodePackage, no Publish folder.

## Success criteria

- [x] Spike_03 is a copy of Spike_02; Spike_02 untouched
- [x] Analyst aborts out-of-bounds needs ASAP (pause to read message; no SR publish)
- [x] Planner / Coder hand off by artifact id only (thesis tests)
- [x] CodePackage schema-validated; Publisher writes `Publish/{workflowId}/` immutably
- [x] Slim host: pauses after Analyst/Planner/Coder; progress logs during LLM/build
- [x] `dotnet build` succeeds in Publisher tests (CI-style); functional run checks deferred to Tester
- [x] `dotnet build` succeeds in a live demo run
- [x] Demo shows artifacts + Publish folder for a tiny console need

## Next after Spike_03

Promote proven stack → `Athlon.*` ([PromotionPlan.md](../../PromotionPlan.md)), then PoC Sprint 1.
