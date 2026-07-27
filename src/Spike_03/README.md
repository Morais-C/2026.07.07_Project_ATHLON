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

Same pattern as Spike_02: `appsettings.json` + gitignored `appsettings.Local.json` (after Phase 0 copy).

## Success criteria

- [ ] Spike_03 is a copy of Spike_02; Spike_02 untouched
- [ ] Analyst aborts out-of-bounds needs ASAP (pause to read message; no SR publish)
- [ ] Planner / Coder hand off by artifact id only (thesis tests)
- [ ] CodePackage schema-validated; Publisher writes `Publish/{workflowId}/` immutably
- [ ] `dotnet build` + run check succeed in tests (CI-style) and demo
- [ ] Slim host: pauses after Analyst/Planner/Coder; progress logs during LLM/build
- [ ] Demo shows artifacts + Publish folder for a tiny console need

## Next after Spike_03

Promote proven stack → `Athlon.*` ([PromotionPlan.md](../../PromotionPlan.md)), then PoC Sprint 1.
