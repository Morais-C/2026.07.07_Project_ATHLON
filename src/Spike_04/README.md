# Spike_04 — Change existing console via artifacts

> **Depends on:** Spike_03 complete (frozen at [`../Spike_03/`](../Spike_03/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — Spike_04 **before** promotion  
> **Host:** Minimal console + real OpenRouter LLM; **fail fast** (no Enter pauses)  
> **Phase progress:** [ImplementationPlan.md §9 Checklist](./ImplementationPlan.md#9-checklist-tracker)

## Purpose

Spike_04 answers:

**Can agents change an existing console via artifacts (not chat), ending in a new buildable Publish tree?**

```text
Fixture baseline (checked-in console)
        +
ChangeRequest (feature | bugfix — one shape)
        ↓
  AnalystAgent   →  StructuredChange (or abort)
        ↓
  CodeContext    →  (deterministic: all fixture files + caps)
        ↓
  PlannerAgent   →  ImplementationPlan
        ↓
  CoderAgent     →  PatchPackage (unified diffs)
        ↓
  Applier (deterministic)
        ↓
  Publish/{workflowId}/  (copy baseline → apply → dotnet build)
```

Spike_03 proved greenfield → Publish.  
Spike_04 proves **incremental** change (like a future git branch) on a known baseline.

## Bounds (fail ASAP)

Same product bounds as Spike_03: single .NET 9 console, read→process→print, ≤3 source files + `.csproj` **after** the change.  
No web/GUI/DB/network-as-feature. Out of bounds → Analyst fails without publishing StructuredChange.

## Pre-locked decisions

See [ImplementationPlan.md §2](./ImplementationPlan.md#2-pre-locked-decisions-2026-07-29) (L1–L14). Do not reopen mid-spike.

## What this spike does *not* include

Promotion, portal, API, SQL, LangGraph, RAG, MCP, git, editing Spike_01–03, Enter pauses, Tester/functional run, unified-diff → full-file fallback (unless amended).

## Prerequisites / config

Same as Spike_03: `appsettings.json` + gitignored `appsettings.Local.json`.

## Success criteria

- [ ] Spike_04 forked from Spike_03; Spike_01–03 untouched
- [ ] Checked-in fixture baseline; ChangeRequest supports feature **and** bugfix (one schema)
- [ ] CodeContext published (all fixture files) with hard caps; over-cap aborts
- [ ] Coder emits PatchPackage (unified diffs); handoff by artifact id only (thesis tests)
- [ ] Applier copies baseline → applies patches → `dotnet build` under new `Publish/{workflowId}/`
- [ ] Fail fast: any step failure stops the host (no Enter pauses)
- [ ] Demo: hardcoded feature **or** bugfix change request → green build in Publish/

## Next after Spike_04

Promote proven stack → `Athlon.*` ([PromotionPlan.md](../../PromotionPlan.md)), then PoC Sprint 1.
